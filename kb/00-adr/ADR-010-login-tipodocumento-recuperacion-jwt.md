---
status: accepted
owner: rubino-f
updated: 2026-09-17
---

# ADR-010 — Login pasa de `codigoPersona` a `tipoDocumento`+`documento`, recuperación de contraseña deja de depender de LDAP

> **Retrospectivo.** El cambio de login se reconstruyó del historial; el motivo no surgía de los commits y fue **confirmado por el equipo el 2026-09-17**, elevando la confianza de `medium` a `alta`. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-010--login-pasa-de-codigopersona-a-tipodocumentodocumento-con-recuperaciónactivación-de-contraseña-por-link-jwt-reemplaza-reset-directo-vía-ldap) — candidato `ARCH-HIST-010`.

## Estado

`accepted` (2026-09-17). Motivación e impacto en usuarios legacy confirmados por el equipo el 2026-09-17.

## Contexto

Hasta el 2026-05-19, el login de `api-admisiones` requería `codigoPersona` (identificador interno, probablemente ligado a LDAP/legacy). En paralelo, existía un flujo de auto-registro de postulantes nuevos que no necesariamente tenían ese código asignado de antemano.

## Decisión

1. `9f725f4c`/`0b7ff878` (2026-05-20): el login pasa a requerir `tipoDocumento`+`documento` en vez de `codigoPersona`.
2. `5a2a8377` (2026-05-20): la recuperación de contraseña deja de depender de un reset directo vía LDAP y pasa a un flujo propio de link JWT por email (cita textual del commit: "replacing direct LDAP resets").

**Motivación (confirmada por el equipo, 2026-09-17):** el auto-registro lo exigía. Un postulante nuevo no tiene `codigoPersona` hasta existir en el sistema, por lo que pedirlo como credencial de login era bloqueante para el flujo de registro público.

## Consecuencias

- El login ya no depende de tener un `codigoPersona` previo, habilitando el auto-registro de postulantes nuevos sin fricción.
- El reset de contraseña reduce su acoplamiento con LDAP para ese flujo puntual (LDAP se sigue usando para autenticación en sí, ver `ADR` de `ARCH-HIST-004`).

## Gaps cerrados (2026-09-17, confirmación del equipo)

- **Usuarios legacy:** el cambio no rompió a nadie. El documento es dato obligatorio en `T_PERSONA`, así que toda persona preexistente podía loguearse con el nuevo esquema. `codigoPersona` sigue existiendo como identificador interno, pero dejó de ser credencial de login.
- **Motivación:** confirmada (ver Decisión) — era un bloqueo real del auto-registro, no una preferencia de usabilidad.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-010`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-010--login-pasa-de-codigopersona-a-tipodocumentodocumento-con-recuperaciónactivación-de-contraseña-por-link-jwt-reemplaza-reset-directo-vía-ldap)
