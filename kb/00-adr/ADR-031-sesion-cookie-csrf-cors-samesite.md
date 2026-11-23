---
status: accepted
owner: gabriele
updated: 2026-11-18
---

# ADR-031 — Sesión por cookie: CSRF por `Origin`, CORS y `SameSite` por ambiente, refresh host-only

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-22 (V-01, V-02, V-05) y 2026-11-18 11:01/11:20 (PT-M2).

## Estado

`accepted` (2026-10-22; cookies host-only el 2026-11-18).

## Contexto

La sesión viaja en cookies (`X-Access-Token`, `X-Refresh-Token`, `X-Password-Activation`). Front y API son *same-site* (`*.ort.edu.uy`). La auditoría [`AUDITORIA-INTEGRACION-ANGULAR-ASPNET.md`](../../security/reports/AUDITORIA-INTEGRACION-ANGULAR-ASPNET.md) encontró:

- escrituras sin protección CSRF;
- una lista de CORS común a todos los ambientes, con `localhost:4200`;
- `SameSite=None` cuando faltaba `ASPNETCORE_ENVIRONMENT`.

El pentest interno ([`PENTEST-INTERNO-2026-11.md`](../../api-admisiones/docs/PENTEST-INTERNO-2026-11.md)) sumó PT-M2: todas las cookies salían con `Domain=.ort.edu.uy`.

## Problema

¿Cómo se evita que otro sitio, o un subdominio de ORT, use la cookie de sesión sin cambiar el front?

## Opciones consideradas

- **Token antiforgery:** exige cambios en el front.
- **`Sec-Fetch-Site`:** no distingue al front legítimo de un subdominio atacante, porque los dos son `same-site`.
- **`Origin` exacto contra la lista de CORS:** ASVS 5.0 3.5.2 lo acepta y no toca el front. Es la opción elegida.

## Decisión

- **CSRF (V-01):** el middleware `UseOriginCheck` corre después de `UseCors`. Un POST/PUT/PATCH/DELETE que trae `Origin` o `Sec-Fetch-Site` recibe 403 si el origen no está en `Cors:AllowedOrigins`. La excepción es `Sec-Fetch-Site: same-origin` (Swagger UI). Un request sin esos headers pasa: no viene de un navegador y no lleva cookies ajenas.
- **CORS (V-02):** `Cors:AllowedOrigins` va en cada `appsettings.{env}.json`, no en el base, porque los arrays de configuración se mezclan por índice. Sin la sección no hay orígenes permitidos (falla cerrado). `localhost:4200` queda solo en `Development` y `LocalHost`.
- **`SameSite` (V-05):** sale de `Authentication:CookieSameSite`, con `Strict` por defecto. `None` queda solo en `Development` y `LocalHost`, que necesitan el front local *cross-site*. Se configura así y no por nombre de ambiente porque `Development` incluye a la vez desa desplegado y los perfiles locales.
- **Alcance de las cookies (PT-M2):** el refresh y la activación pasan a host-only, sin `Domain`. El access token sigue con `Authentication:CookieDomain`, porque `@desarrolloort/fdp-components` llama a la API de Ficha de Persona con `withCredentials` y se autentica con él. `DeleteLegacyDomainCookie` borra la versión vieja con dominio cada vez que se emite o se limpia la cookie.

## Consecuencias

- Con V-01 cerrado, `SameSite=None` en desa ya no permite CSRF.
- El access token sigue visible para cualquier subdominio de `.ort.edu.uy`. Achicarlo depende de FDP.
- `DeleteLegacyDomainCookie` se puede borrar después de `Jwt:RefreshExpireDays` desde el despliegue en producción.
- El token antiforgery queda como defensa en profundidad, solo si un pentest lo pide.
- Tests: `CsrfOriginValidationTests`, `CorsPolicyByEnvironmentTests`, `CookieSameSiteTests`, `CookieDomainScopeTests`.

## Referencias

- [ADR-032](ADR-032-revocacion-sesiones.md): revocación de sesión (V-04).
- [ADR-043](ADR-043-produccion-cerrado-por-defecto.md): checklist de producción (dominio y `SameSite`).
