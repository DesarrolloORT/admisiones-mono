---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-024 — "Mis becas": el estado lo resuelve la vista y el back manda solo lo que muestra cada card

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-01, 2026-10-06 y 2026-10-16.

## Estado

`accepted` (2026-10-16).

## Contexto

`GET person/scholarships` era un volcado de 26 campos de `VD_ADMISIONES_MIS_BECAS`, y el front decidía qué card mostrar con los estados crudos (`ESTADO_INSCRIPTO_PRUEBA`, `ESTADO_DJ_WEB`). Eso duplicaba reglas de negocio en el front.

## Decisión

- **La vista calcula el estado** (`ESTADO_PARA_ADMISIONES`). El C# solo lo traduce al enum `MyScholarshipStatus`, con `Trim()` y sin distinguir mayúsculas. Con o sin prueba sale de `REQUIERE_RENDIR_PRUEBA`, no de la fecha de examen.
- **Cada card recibe solo sus campos**, en bloques (`application`, `exam`, …). Lo null no se serializa, con `JsonIgnore` por propiedad, no global.
- **Estado desconocido: no se muestra y se loguea un warning** (reemplaza el fallback a `InProgress` del 2026-10-01, que prometía "Continuar postulación" sin saber si correspondía).
- `CADUCA` se filtra en el back: solo la ve el backoffice.
- Un campo que otra pantalla necesite se agrega al bloque y al estado que corresponde, no como campo plano para todos.

## Consecuencias

- Agregar un estado es tocar la vista y el switch de `MyScholarshipsMapper.ToStatus`.
- **Breaking con el front:** forma de la respuesta (2026-10-01) y `Completed` → `Confirmed` (2026-10-16), en `MATRIZ-TRAZABILIDAD.md` y `becas.contract.json`.
- El accept valida `ESTADO_INSCRIPTO_PRUEBA = 'A'` sobre la tabla y la card dice "Otorgada" según la vista: tienen que coincidir.
- Pendientes: diseño de la card `NoShow`; URL del material de estudio; si `AwaitingResult` termina siendo la misma card que `Confirmed`, se borra el enum.

## Referencias

- [ADR-021](ADR-021-becas-paridad-escrituras-legacy.md) — escrituras de becas
- [ADR-017](ADR-017-result-t-formato-unico-error.md) — formato de respuesta
