---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-025 — Becas: para postular hay que estar inscripto

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-01 (Parte B), 2026-10-06 14:00 y 2026-10-07.

## Estado

`accepted` (2026-10-01).

## Contexto

En el sitio viejo se podía postular a una beca sin estar inscripto a la carrera: la DJ quedaba en `CONFIRMADO_WEB_SIN_INSCRIPCION`, no se espejaba al CRM y el confirm corría `ModificacionInteres` para crear o subir el interés.

## Decisión

**Sin inscripción no hay postulación**, controle la prueba o no (`CONTROL_INSCR_PRUEBA` deja de importar).

- Las opciones y el alta leen `VD_PRUEBAS_DISPONIBLES`, que exige una inscripción paga y permite un solo fondo por grupo (26/33/57) y comienzo.
- Confirmar la DJ sin inscripción (baja entre la postulación y el confirm) → 409 `FDB_GDJ_51`, sin escribir nada. Siempre queda `CONFIRMADO_WEB`.
- El confirm no corre `ModificacionInteres`: el interés y su grado ya los resolvió la inscripción, y no se desinteresan los productos hermanos (la persona puede inscribirse a varias carreras).
- `AffidavitState.ConfirmadoSinInscripcion` se conserva **solo para leer**: si dejara de contar como confirmada, las DJ del sitio viejo volverían a ser editables.
- Inscripto con la vista vacía (todos sus fondos bloqueados por grupo) → `RequiresPriorEnrollment = false` con lista vacía, no el catálogo de "inscribite primero".

## Consecuencias

- **Hueco aceptado:** `canConfirm` del `GET affidavit` no mira la inscripción; en el caso borde de una baja, el botón se habilita y el confirm responde 409. Sumar la consulta le agregaría una lectura a cada carga del formulario.
- Si el espejo al CRM no encuentra interés, se saltea sin error (dato inconsistente del CRM).
- La vista tiene que contar como completadas las postulaciones viejas en `CONFIRMADO_WEB_SIN_INSCRIPCION`.
- Pendiente: el empty state del front para `false` con lista vacía.

## Referencias

- [ADR-021](ADR-021-becas-paridad-escrituras-legacy.md) — paridad con el legacy
- `api-admisiones/docs/BECAS-DECLARACION-JURADA.md` (regla 16)
