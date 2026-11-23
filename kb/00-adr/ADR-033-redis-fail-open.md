---
status: accepted
owner: gabriele
updated: 2026-11-17
---

# ADR-033 — Los controles que viven en Redis fallan abiertos; el 2FA falla cerrado

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-30 (L-07), 2026-11-13 08:43 (SEG-L1-01, TRV-06), 2026-11-17 09:09 (SEG-L4-01) y 09:33 (SEG-L5-01).

## Estado

`accepted` como **riesgo aceptado** (2026-10-30).

## Contexto

Redis ([ADR-005](ADR-005-redis-cache-y-estado.md)) sostiene el rate limiting, la revocación de tokens, el candado entre réplicas y las sesiones de 2FA. No es un servicio de alta disponibilidad.

## Problema

Con Redis caído, ¿se rechazan los requests o se dejan pasar?

## Opciones consideradas

- **Fail-closed para todo:** una caída de Redis tumba la autenticación, las reservas y los pagos de todos.
- **Fail-closed solo para el login:** Redis pasa a ser un punto único de falla del login.
- **Limitador en memoria como respaldo:** el límite queda por réplica y es una pieza más para mantener.
- **Fail-open con log:** prioriza la disponibilidad. Es la opción elegida.

## Decisión

Cuando Redis falla, estos controles dejan pasar y loguean:

| Control | Log |
|---|---|
| Rate limiting (`RedisRateLimiterService.IsAllowedAsync`) | `"Failing open"`, nivel `Error` |
| Revocación de access y reuso del refresh ([ADR-032](ADR-032-revocacion-sesiones.md)) | warning |
| Candado distribuido ([ADR-036](ADR-036-candado-distribuido.md)) | warning |

El **2FA falla cerrado**: sus sesiones viven en Redis y sin ellas no hay código para validar.

## Consecuencias

- Con Redis caído no hay límites contra fuerza bruta en el login. Mitiga en parte que el access vence solo en `Jwt:ExpireMinutes`.
- No hay alerta: no hay acceso al Grafana de producción. La caída se ve en Loki por el log de nivel `Error`.
- Los tests fijan el fail-open a propósito. Cambiarlo es decidir de nuevo, no arreglar un bug.

## Referencias

- [ADR-023](ADR-023-endpoints-anonimos-defaults-seguros.md): rate limit con Lua.
