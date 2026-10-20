---
status: accepted
owner: gabriele
updated: 2026-10-20
---

# ADR-030 — Link del backoffice: el `codigoPersona` del token tiene que ser el de la solicitud

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entrada del 2026-10-20 (link del backoffice).

## Estado

`accepted` (2026-10-20).

## Contexto

Cuando el backoffice aprueba una solicitud de alta, manda un link con un JWT firmado que trae `idSolicitudAlta`. Si la persona ya existe en `T_PERSONA`, trae además `codigoPersona`. Con ese claim, la API pisa a esa persona con los datos de la solicitud y le fija la contraseña.

La API solo verificaba que la persona del claim existiera, no que fuera la persona de la solicitud. Desde [ADR-029](ADR-029-registro-datos-no-coinciden-solicitud-alta.md), las solicitudes que crea `verify-identity` ya traen `CODIGO_PERSONA`, así que la API puede hacer esa comparación.

## Problema

Un error del backoffice, como mandar la solicitud de una persona con el código de otra, sobrescribiría el nombre, el mail y el documento de esa otra persona. Además, quien recibiera el link le crearía la contraseña.

## Decisión

Al abrir el link (`ActivateBackofficeRequestToken`), si la solicitud tiene `CODIGO_PERSONA` y el token trae otro código:

- responde 401 `Authentication.ActivationTokenWithoutPerson`;
- loguea un warning con el id de la solicitud y los dos códigos.

Se reusa esa clave porque su texto ("El token no contiene una persona válida.") ya describe el caso.

## Consecuencias

- Las solicitudes de extranjeros no tienen `CODIGO_PERSONA`: no cambian.
- Si el token no trae `codigoPersona`, no se toma el de la solicitud: el backoffice siempre lo manda en este caso.
- El token solo lo puede generar el backoffice, porque va firmado con nuestro secreto. Esto protege contra errores de ese sistema, no contra un ataque de afuera.

## Referencias

- [ADR-029](ADR-029-registro-datos-no-coinciden-solicitud-alta.md): solicitud por datos que no coinciden.
- `api-admisiones/WebApiAdmisiones/AppLogic/AppLogic.Authentication/README.md`
