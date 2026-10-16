---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-023 — Endpoints anónimos: cerrado por defecto, rate limit en el borde y bloqueo por documento alto

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-09-16 (HOST-01, HOST-02, HOST-03, HOST-04, AUTH-01, AUTH-05, PLA-01).

## Estado

`accepted` (2026-09-16). Complementa [ADR-009](ADR-009-hardening-seguridad-api-publica.md) sin modificarlo.

## Contexto

El code review de `docs/code-review/` encontró controllers abiertos por defecto, endpoints anónimos sin freno de volumen, un bloqueo por documento explotable como DoS y un rate limit que nunca se reseteaba.

## Decisión

- **`[Authorize]` de clase en todos los controllers.** Cada action anónima lleva `[AllowAnonymous]` explícito. Un test por reflexión falla si una action nueva queda abierta sin declararlo.
- **`PublicAuthRateLimitPolicy`** en `verify-identity`, `confirm-new-person`, `confirm-registration-request`, `verify-two-factor-code` y `resend-two-factor-code`.
- **`refresh-token` queda sin esa política, a propósito:** se llama durante toda la sesión y compartir el balde bloquearía sesiones detrás de un NAT (WiFi de campus). Necesita una política propia; un test fija que hoy no la tiene.
- **Bloqueo por documento: de 5 a 30 intentos en 15 min.** No se elimina (es la única protección contra fuerza bruta con rotación de IPs), pero deja de ser un DoS con 5 requests. IP+documento (5/15 min) sigue haciendo el trabajo principal.
- **Rate limit con script Lua (`EVAL`):** cuenta, decide y recién ahí agrega. Un rechazo ya no corre la ventana.
- **Logout revoca el refresh token**, best-effort: el contrato con el front no cambia.
- **IP del cliente en un solo helper.** `KnownProxies` y la partición del rate limit **no** se tocan hasta que infra confirme la topología del proxy.
- **HOST-02 descartado:** `Testing` está solo en la red interna. Se reabre si eso cambia.

## Consecuencias

- El front puede ver 429 en cinco endpoints donde antes no.
- La atomicidad del script Lua no se prueba contra un Redis real: no hay tests de integración.
- Pendiente: política de rate limit para `refresh-token` y `KnownProxies`/`KnownNetworks`.

## Referencias

- `api-admisiones/docs/code-review/01-WebApiAdmisiones-host.md`, `02-Authentication.md`, `10-Platform.md`
- [ADR-005](ADR-005-redis-cache-y-estado.md) — Redis
