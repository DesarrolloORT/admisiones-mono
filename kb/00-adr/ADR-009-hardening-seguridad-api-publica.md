---
status: draft
owner: rubino-f
updated: 2026-09-17
---

# ADR-009 — Endurecimiento iterativo de seguridad de la API pública (rate limiting, CORS, CAPTCHA, SameSite) hasta un diseño anti-enumeración maduro

> **Retrospectivo, confianza `medium`.** El patrón de cambios está claro; la motivación de varias reversiones puntuales (CORS de 2 días, CAPTCHA prendido/apagado, `SameSite` inestable) es inferencia, no confirmada. **No promover a `accepted` sin que el equipo confirme el estado actual en producción.** Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-009--endurecimiento-de-seguridad-de-api-pública-rate-limiting-cors-credentials-captcha-condicional-por-ambiente--con-reversión-de-cors-en-2-días) — candidato `ARCH-HIST-009`.

## Estado

`draft` — **se mantiene deliberadamente en `draft` tras la revisión del 2026-09-17.** El estado vigente se verificó en código (ver sección siguiente) y aparecieron tres hallazgos que contradicen la premisa de "diseño maduro" del ADR. No se acepta hasta resolverlos o asumirlos explícitamente.

## Contexto

Entre mayo y agosto de 2026, la superficie pública de `api-admisiones` (login, registro, recuperación de contraseña, reconocimiento de documentos) pasó por varios ciclos de endurecimiento de seguridad, algunos con reversiones sin explicación documentada.

## Cronología de la decisión

1. **Mayo**: primer rate limiting (en memoria) y CAPTCHA condicional. `CORS DisallowCredentials()` revertido a `AllowCredentials()` en 2 días sin explicación.
2. **Mayo-junio**: CAPTCHA prendido y apagado al menos 5 veces; `SameSite` de cookies cambiado entre `Strict`/`None` repetidamente "para desarrollo", sin garantía verificada de diferenciación real por ambiente.
3. **Julio** (`88f35a82`): se resuelve la inestabilidad de JWT/cookies — `SameSite` dinámico por ambiente, JWT restringido a HS256 con secreto mínimo de 32 bytes.
4. **Agosto** (`4ef193e5`): rate limiting unificado vía Redis para login/registro/recuperación (30 req/15min/IP compartido) con respuestas de error genéricas, diseñado explícitamente para **prevenir enumeración de cuentas** ("closing enumeration oracles").

## Decisión

El estado final (agosto en adelante) es un diseño de seguridad deliberado y maduro (anti-enumeración con rate limiting distribuido), pero llegó ahí después de varios meses de configuración inestable y al menos un cambio de CORS revertido sin registro del motivo.

## Consecuencias

- La superficie pública queda con rate limiting, CAPTCHA y protección anti-enumeración, medibles vía Prometheus.
- **Cosa extraña señalada al equipo**: la reversión de CORS de 2 días (mayo) y las alternancias de CAPTCHA/SameSite no tienen explicación documentada — señal de iteración bajo presión, no de un proceso de cambio revisado.

## Estado vigente verificado en código (2026-09-17)

| Control | Estado | Ubicación |
|---|---|---|
| CORS | `AllowCredentials()`, sin `AllowAnyOrigin`, lista de orígenes hardcodeada | `Extensions/ServiceCollectionExtensions.cs:154-184` |
| SameSite | `Strict` si el ambiente es production-like, `None` en caso contrario | `Security/Authentication/CookieAuthenticationHelper.cs:40-47` |
| Cookies | `Secure = true` y `HttpOnly = true` fijos, no dependen de ambiente | idem, `:59-61, 81-83, 121-122` |
| CAPTCHA | Activo en 7 endpoints, sin flag de bypass; falla cerrado si falta el secreto | `Security/Captcha/RequireCaptchaAttribute.cs`, `RecaptchaService.cs:33-44` |
| JWT | Restringido a HS256, secreto mínimo de 32 bytes validado | `Security/Authentication/AuthenticationExtensions.cs:68, 116-120` |

El punto 3 de la cronología (julio, `88f35a82`) se confirma: JWT y el `SameSite` dinámico están tal como el ADR describe.

## Hallazgos que impiden aceptar este ADR (2026-09-17)

1. **`SameSite=None` es el default por omisión.** `CookieAuthenticationHelper.cs:42-46` lee `ASPNETCORE_ENVIRONMENT` crudo del proceso y compara por string contra `"Production"`/`"Preproduction"`. Si la variable falta, está vacía o dice otra cosa (`"Prod"`, `"PRODUCTION-01"`), el resultado es `SameSiteMode.None` — el modo más permisivo, con la cookie de sesión viajando cross-site. Es un control de seguridad que se desactiva solo ante una configuración ausente, que es exactamente el modo de falla que no se quiere. Agrava el problema que el proyecto **ya tiene** el helper `EnvironmentExtensions.IsProductionLike` (`Extensions/EnvironmentExtensions.cs:15`), usado en el resto de la app: acá la lógica está duplicada a mano en vez de reutilizarlo.

2. **CORS acepta credenciales desde `localhost` en producción.** La lista de orígenes (`ServiceCollectionExtensions.cs:156-167`) es única para todos los ambientes e incluye `http://localhost:4200` y `http://localhost:5001` (HTTP plano), más todos los subdominios no productivos (`admisionesdesa`, `admisionestesting`, `admisionespreprod`). Combinado con `AllowCredentials()`, en producción se admiten requests con cookies desde esos orígenes. Debería segmentarse por ambiente.

3. **El umbral de reCAPTCHA no bloquea ningún endpoint.** Los 7 usos de `[RequireCaptcha]` (`AuthController.cs:72, 382`; `RegistrationController.cs:74, 111, 163, 256, 284`) usan `CaptchaValidationMode.ScoreOnly`. La rama que compara contra `RECAPTCHA_SCORE` (`RequireCaptchaAttribute.cs:88-97`) solo corre con `RequireMinimumScore` y hoy es **código muerto**. En la práctica reCAPTCHA valida que el token sea legítimo, pero un score bajo no rechaza la request: solo dispara el 2FA adaptativo del login (ver `ADR-011`). Además el default `0.5` está duplicado en `RequireCaptchaAttribute.cs:89` y `LoginFlowService.cs:131`, y `RECAPTCHA_SCORE` no figura en ningún `appsettings*.json` — vive solo como variable de entorno con fallback silencioso.

Punto relacionado: `JWT_SECRET_KEY` y `RECAPTCHA_SECRET_KEY` no están en `RequiredConfigurationExtensions.RequiredKeys` (`:20-24`), así que la app levanta sin ellos y falla recién en el primer login. Falla cerrado, pero tarde.

## Gaps abiertos

- Qué rompió `DisallowCredentials()` en mayo — no es verificable en código, requiere memoria del equipo.
- Decidir sobre los tres hallazgos de arriba: corregir, o asumirlos explícitamente como riesgo aceptado con su justificación.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-009`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-009--endurecimiento-de-seguridad-de-api-pública-rate-limiting-cors-credentials-captcha-condicional-por-ambiente--con-reversión-de-cors-en-2-días)
- Relacionado: `ADR-005` (Redis), `ADR-010` (2FA)
