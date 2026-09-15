---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-009 — Endurecimiento iterativo de seguridad de la API pública (rate limiting, CORS, CAPTCHA, SameSite) hasta un diseño anti-enumeración maduro

> **Retrospectivo, confianza `medium`.** El patrón de cambios está claro; la motivación de varias reversiones puntuales (CORS de 2 días, CAPTCHA prendido/apagado, `SameSite` inestable) es inferencia, no confirmada. **No promover a `accepted` sin que el equipo confirme el estado actual en producción.** Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-009--endurecimiento-de-seguridad-de-api-pública-rate-limiting-cors-credentials-captcha-condicional-por-ambiente--con-reversión-de-cors-en-2-días) — candidato `ARCH-HIST-009`.

## Estado

`draft` — pendiente de validación. **Antes de aceptar, el equipo debe confirmar la configuración vigente hoy en producción** (CORS credentials, CAPTCHA, SameSite) — el historial muestra iteración inestable, no un estado final confiable por sí solo.

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

## Gaps

- Confirmar con el equipo qué rompió `DisallowCredentials()` en mayo.
- **Confirmar el estado actual (no solo histórico) de CORS credentials, CAPTCHA y SameSite en producción** antes de dar por buena cualquier configuración vista en el historial.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-009`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-009--endurecimiento-de-seguridad-de-api-pública-rate-limiting-cors-credentials-captcha-condicional-por-ambiente--con-reversión-de-cors-en-2-días)
- Relacionado: `ADR-005` (Redis), `ADR-010` (2FA)
