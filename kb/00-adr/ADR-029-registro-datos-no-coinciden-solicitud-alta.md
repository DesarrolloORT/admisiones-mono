---
status: accepted
owner: gabriele
updated: 2026-10-20
---

# ADR-029 — Registro: si el apellido o el mail no coinciden con el padrón, se crea una solicitud de alta

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entrada del 2026-10-20 (`verify-identity`).

## Estado

`accepted` (2026-10-20).

## Contexto

Una persona con cédula que ya está en `T_PERSONA` y no tiene usuario LDAP se registra con `POST registration/verify-identity`: tiene que ingresar el apellido y el mail del padrón. Si alguno no coincidía, la API respondía 400 `Registration.IdentityDataMismatch` sin dejar nada escrito. Quien cambió de mail o lo escribía distinto quedaba trabado, sin camino a revisión manual: `confirm-registration-request` solo acepta documentos extranjeros.

## Problema

¿Cómo se registra alguien del padrón cuyos datos no coinciden, sin abrir un camino para tomar la cuenta de otra persona?

## Opciones consideradas

- **Seguir con el 400.** Descartada: la persona no tiene salida.
- **Aceptar los datos ingresados y crear el usuario.** Descartada: cualquiera que conozca un documento podría registrarse con su propio mail.
- **Solicitud de alta para revisión del backoffice.** Elegida: es el mismo mecanismo que ya se usa para los documentos extranjeros.

## Decisión

- **Del lado de la API (`VerifyIdentity`):** si no coinciden el apellido, el mail o ambos, crea una fila en `T_SOLICITUD_ALTA` y su fila en `T_REGISTRO_ADMISIONES`. La solicitud lleva:
  - `CODIGO_PERSONA`, documento y tipo.
  - El apellido y el mail **que ingresó** la persona.
  - El primer nombre del padrón, porque la columna es NOT NULL y el endpoint no lo pide.
- No se crea usuario LDAP ni se manda mail.
- Si ya hay una solicitud para el documento, no se crea otra: responde "en revisión".
- Responde 200 con `pendingReview: true` y `mailSent: false`, igual que la solicitud de un extranjero.
- **Del lado del backoffice:** consolida los datos en `T_SOLICITUD_ALTA` y manda el mail con el link. El JWT lleva `idSolicitudAlta` y `codigoPersona`.
- **Al crear la contraseña:** la API usa el camino que ya existía para personas existentes (`UpdateExistingPersonFromRequestAsync`). Actualiza `T_PERSONA` con lo que tenga valor en la solicitud, crea el usuario LDAP, registra la admisión y borra la solicitud. La API no manda mail en este camino.
- El 400 `IdentityDataMismatch` queda solo para la persona que ya no está en el padrón: sin persona no hay `CODIGO_PERSONA`.

## Consecuencias

- **Breaking con el front:** donde había un 400 ahora hay un 200 con `pendingReview: true`. El front tiene que mostrar la pantalla de "queda en revisión". Está en `registro.contract.json` (versión 3) y en `MATRIZ-TRAZABILIDAD.md`.
- Lo que deja el backoffice en la solicitud es lo que queda en `T_PERSONA`. Si no corrige el apellido o el mail, quedan los que ingresó la persona.
- La solicitud no dice por qué se creó. Si el backoffice lo necesita, se puede usar `OBSERVACIONES_SOLICITUD_ALTA`.
- El chequeo de duplicado busca por documento y no mira el estado de la solicitud, igual que `evaluate-document`.

## Referencias

- [ADR-030](ADR-030-link-backoffice-persona-de-la-solicitud.md): validación del `codigoPersona` del link.
- [ADR-014](ADR-014-autenticacion-ldap-jwt-combinada.md): autenticación LDAP + JWT.
- `api-admisiones/WebApiAdmisiones/AppLogic/AppLogic.Registration/README.md`
