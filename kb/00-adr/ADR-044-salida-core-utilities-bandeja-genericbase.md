---
status: accepted
owner: gabriele
updated: 2026-10-21
---

# ADR-044 — Admisiones deja `Core/Utilities`, ModBandeja y ModGenericBase; la bandeja entra en la transacción de admisiones

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-21 (`Core/Utilities`, `BandejaService`, ModBandeja, ModGenericBase).

## Estado

`accepted` (2026-10-21). Continúa [ADR-020](ADR-020-core-a-packages-ort.md).

## Contexto

Después de [ADR-020](ADR-020-core-a-packages-ort.md), admisiones seguía referenciando `Core/Utilities` desde 12 csproj, pero usaba apenas unos 11 de sus ~600 miembros. ModBandeja y ModGenericBase llegaban por un solo uso cada uno. El trámite corporativo se guardaba con su propio `Save`, en otro DbContext y antes del commit de admisiones.

## Decisión

- **Se trae solo lo usado y el comportamiento no cambia:**
  - `PasswordRules.ValidateNew`/`ValidateChange` mantienen la misma regex y los mismos mensajes;
  - `TextNormalization.ToUpperWithoutAccents` sigue quitando solo las 10 vocales con tilde;
  - `LdapServiceKeyProvider.Deobfuscate` sigue el mismo algoritmo;
  - las constantes de interés van a `EnrollmentConstants`.
- **Bandeja en el modelo Devart de admisiones:** `T_TRAMITE_BANDEJA` y `T_BANDEJA` se suman al modelo, con mapeos de auditoría idénticos a los de `BandejaModelContext`. `PreEnrollmentConfirmationRules.AddCorporateCase` agrega trámite, instancia y bandejas al `IUnitOfWork` de admisiones, y el `Commit` del caso de uso confirma todo junto.
- **`PASSWORD_LDAP`** se lee de `CREADOR.T_PARAMETROS` con el `IUnitOfWorkFactory` de admisiones.
- Salen del `.sln`, de los csproj y del Dockerfile: Utilities, ModBandeja y ModGenericBase. Del Core solo queda `ConnectionContext`.

## Consecuencias

- **Arreglo:** antes, si fallaba el commit de admisiones, quedaba un trámite en la bandeja sin fila en `T_INST_WORKFLOW_INSCRIPCION`. Ahora las cuatro tablas van en una sola transacción.
- Los errores `FILE_VAL_*` del reconocimiento de documento pasan a inglés, como en el resto de las subidas.
- `.github/copilot-instructions.md` (el baseline compartido) todavía cita `OperationResult<T>`. Ya se corrigió en la capa de verificación ([ADR-045](ADR-045-capa-verificacion-agente.md)).
- **Pendiente:** confirmar a mano contra Oracle una preinscripción corporativa (1 y 2 ofertas) y un login LDAP de punta a punta.

## Referencias

- [ADR-007](ADR-007-persistencia-devart-oracle.md): Devart.
- [ADR-017](ADR-017-result-t-formato-unico-error.md): `Result<T>`.
