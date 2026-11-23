---
status: accepted
owner: gabriele
updated: 2026-10-22
---

# ADR-047 — "Mis becas": `AwaitingResolution` lo resuelve el mapper, como excepción a ADR-024

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entrada del 2026-10-22 14:27.

## Estado

`accepted` (2026-10-22). Es una excepción acotada a [ADR-024](ADR-024-mis-becas-estado-desde-vista.md), que sigue vigente.

## Contexto

Hasta que se carga el puntaje, `ESTADO_INSCRIPTO_PRUEBA` sigue en `E`. Pasada la fecha de la prueba, la card llegaba como `Confirmed` y el front volvía a mostrar "Descargar material". El front pidió un estado "Esperando resolución".

## Opciones consideradas

- **Que lo calcule la vista**, como pide ADR-024: depende del DBA.
- **Que lo calcule el mapper:** sale sin esperar al DBA. Es la opción elegida.

## Decisión

- Se agrega el valor `AwaitingResolution` al enum, con los mismos bloques que `Confirmed`. No se reusa `AwaitingResult`, que corresponde a `A LA ESPERA`, con el puntaje ya cargado.
- Regla en `MyScholarshipsMapper`: `Confirmed` + `REQUIERE_RENDIR_PRUEBA = SI` + `FECHA_EXAMEN_PRUEBA < hoy` → `AwaitingResolution`. Se compara por día.
- No hay tope por `FECHA_RESULTADO_PRUEBA`: volver a `Confirmed` mostraría de nuevo "Descargar material".

## Consecuencias

- Cuando la vista cambia de estado, la regla deja de aplicar.
- Con el contenedor en UTC, el cambio se ve desde las 21:00 de Montevideo del día de la prueba (BEC-10).
- Si negocio prefiere que lo resuelva la vista, el mapper vuelve a una línea en `ToStatus`.
- Contrato: `becas.contract.json` (`breakingChanges`) y `MATRIZ-TRAZABILIDAD.md`.

## Referencias

- [ADR-024](ADR-024-mis-becas-estado-desde-vista.md)
