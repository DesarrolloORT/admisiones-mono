---
status: accepted
owner: gabriele
updated: 2026-11-17
---

# ADR-036 — Candado distribuido en Redis para las operaciones que no se pueden repetir

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-11-13 10:11 (SEG-L2-06) y 2026-11-17 09:33 (SEG-L5-01, SEG-L5-02, SEG-L5-03).

## Estado

`accepted` (2026-11-17).

## Contexto

La API corre en varias réplicas. Algunas operaciones leen un estado, lo chequean y después escriben, sin nada en el medio. Un doble clic o dos requests en paralelo pasaban dos veces el chequeo:

| Operación | Efecto del doble envío |
|---|---|
| `complete-initial-password` | Dos personas o dos usuarios LDAP |
| Reserva de entrevista | Dos personas en el mismo horario, o una persona en varios |
| Confirmar la DJ | Espejo a SGI y aviso a Tivenos duplicados |
| Preinscripción y pago | Trámites o pagos duplicados |

## Problema

¿Cómo se serializan esas operaciones entre réplicas sin cambiar el esquema de Oracle?

## Decisión

- **`AppLogic.Platform.Locking.IDistributedLock` (`RedisDistributedLock`):** `SET NX` con TTL sobre `LockTake`/`LockRelease`. Solo libera el candado quien lo tomó, así que un TTL vencido no libera el candado de otro. Las claves son `lock:{recurso}`.
- La API es sincrónica porque los servicios que la usan lo son, y `AGENTS.md` prohíbe `.Result`.
- Se toma el candado antes de abrir la base, y el segundo request recibe 409:

| Operación | Clave | 409 |
|---|---|---|
| Reserva de entrevista | `interview-person:{id}` y `interview-slot:{id}` | `Scholarship.InterviewInProgress` / `InterviewSlotNotAvailable` |
| Confirmar la DJ | `affidavit-confirm:{persona}:{postulación}` | `Affidavit.ConfirmInProgress` |
| Preinscripción | `pre-enrollment:{persona}` | `Enrollment.PreEnrollmentInProgress` |
| Pago | `payment:{persona}` | `Payment.InProgress` |
| Fijar la contraseña inicial | propósito + sujeto de la sesión | `Authentication.PasswordCompletionInProgress` |

- Además se deduplican `SelectedOfferingIds` y `EnrollmentIds`.
- **Sin candado en el PUT de la DJ ni en la subida de adjuntos:** el front sube comprobantes en paralelo y guarda mientras sube. En su lugar, el PUT deja de llamar a `Update()`, y EF escribe solo lo que cambió el mapper.

## Consecuencias

- Si Redis falla, el candado deja pasar ([ADR-033](ADR-033-redis-fail-open.md)).
- **Recomendado aparte:** un UNIQUE en Oracle sobre la reserva activa por horario, que no depende de Redis.
- Al mergear los lotes 2 y 5 conviene dejar solo `IDistributedLock` y borrar el `IOperationLock` de Authentication.
- Esta API no cubre dos casos secuenciales: el segundo pago (lo rechaza la API de Pagos) y el segundo trámite corporativo para la misma oferta (regla de negocio, ENR-01).

## Referencias

- [ADR-005](ADR-005-redis-cache-y-estado.md): Redis.
- [ADR-046](ADR-046-becas-reconsideracion.md): entrevista de reconsideración.
