---
status: accepted
owner: gabriele
updated: 2026-11-18
---

# ADR-032 — Revocación de sesiones: access revocable en Redis y detección de reuso del refresh

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-22 (V-04), 2026-11-17 09:09 (SEG-L4-01) y 2026-11-18 09:34 (PT-M1, PT-L1).

## Estado

`accepted` (2026-11-18).

## Contexto

El access token es un JWT de 15 minutos que se valida sin estado. La base guarda **un solo refresh por persona y sistema**, únicamente el hash vigente. Después del logout o de un cambio de contraseña, el access seguía sirviendo hasta vencer, también por el header `Authorization`, sin la cookie. Un refresh robado y ya rotado no dejaba rastro.

## Problema

¿Cómo se corta una sesión antes de que venza sin pasar a tokens opacos ni tocar `DataAccess`?

## Decisión

- **Sesión nueva al cambiar la contraseña (V-04):** `change-password` emite una sesión nueva. El upsert del refresh invalida cualquier copia robada. `RefreshTokens` aplica `SessionRules.CanHaveSession` y conserva el `ExpiresAt` del login.
- **Access revocable (SEG-L4-01):** `TokenService` emite `jti` e `iat`. Hay dos claves en Redis:
  - el logout escribe `session:revoked-token:{jti}`;
  - el cambio de contraseña escribe `session:revoked-before:{personId}` y así corta la sesión en todos los dispositivos.

  `OnTokenValidated` lee las dos claves con un solo `MGET` y rechaza con 401 sin body.
- **Rotación del refresh (PT-M1, PT-L1):** al rotar, se guarda `session:rotated-refresh:<hash>` y se revoca el `jti` del access anterior. Si un hash rotado vuelve después de `RefreshTokens.ReuseGrace` (30 s), se revocan el refresh vigente y todos los access de la persona, y se loguea un warning. Dentro de los 30 s el request solo se rechaza, para no desloguear a quien tiene dos pestañas que refrescan a la vez.

## Consecuencias

- Cuesta una lectura a Redis por request autenticado.
- Un token sin `iat` (emitido antes de este cambio) se rechaza si la persona tiene un corte.
- Si Redis falla, el token se acepta y la detección de reuso no corre. Ver [ADR-033](ADR-033-redis-fail-open.md).
- Para el front, solo cambia una cosa: un reuso tardío cierra la sesión. Rutas, shapes y status no cambian.
- Sigue afuera la revalidación del estado de la cuenta en LDAP en cada refresh, por el costo de una llamada por renovación.

## Referencias

- [ADR-014](ADR-014-autenticacion-ldap-jwt-combinada.md): LDAP + JWT.
- [ADR-023](ADR-023-endpoints-anonimos-defaults-seguros.md): el logout revoca el refresh.
- `api-admisiones/loadtest/security-smoke.sh`: chequeos `[PT-M1]` y `[PT-L1]`.
