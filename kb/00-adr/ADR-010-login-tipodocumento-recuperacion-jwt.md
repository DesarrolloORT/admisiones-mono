---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-010 — Login pasa de `codigoPersona` a `tipoDocumento`+`documento`, recuperación de contraseña deja de depender de LDAP

> **Retrospectivo, confianza `medium`.** El cambio de login está confirmado; el motivo (por qué `codigoPersona` dejó de ser viable) es inferido, no confirmado. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-010--login-pasa-de-codigopersona-a-tipodocumentodocumento-con-recuperaciónactivación-de-contraseña-por-link-jwt-reemplaza-reset-directo-vía-ldap) — candidato `ARCH-HIST-010`.

## Estado

`draft` — pendiente de validación del equipo.

## Contexto

Hasta el 2026-05-19, el login de `api-admisiones` requería `codigoPersona` (identificador interno, probablemente ligado a LDAP/legacy). En paralelo, existía un flujo de auto-registro de postulantes nuevos que no necesariamente tenían ese código asignado de antemano.

## Decisión

1. `9f725f4c`/`0b7ff878` (2026-05-20): el login pasa a requerir `tipoDocumento`+`documento` en vez de `codigoPersona`.
2. `5a2a8377` (2026-05-20): la recuperación de contraseña deja de depender de un reset directo vía LDAP y pasa a un flujo propio de link JWT por email (cita textual del commit: "replacing direct LDAP resets").

## Consecuencias

- El login ya no depende de tener un `codigoPersona` previo, habilitando el auto-registro de postulantes nuevos sin fricción.
- El reset de contraseña reduce su acoplamiento con LDAP para ese flujo puntual (LDAP se sigue usando para autenticación en sí, ver `ADR` de `ARCH-HIST-004`).

## Gaps

- No está documentado qué pasa con usuarios que sí tienen `codigoPersona` (staff/legacy): confirmar con el equipo funcional si siguen pudiendo loguearse igual o si este cambio los afectó.
- Motivación del cambio de login no explicada en el commit; es inferencia razonable por el contexto de auto-registro, no confirmada.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-010`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-010--login-pasa-de-codigopersona-a-tipodocumentodocumento-con-recuperaciónactivación-de-contraseña-por-link-jwt-reemplaza-reset-directo-vía-ldap)
