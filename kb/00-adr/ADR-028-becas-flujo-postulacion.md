---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-028 — Becas: el alta va por el legacy; la DJ, el resumen y la aceptación por la API nueva

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-09-29 12:15, 2026-09-30, 2026-10-02 09:04 y 2026-10-06 12:30.

## Estado

`accepted` (2026-10-02).

## Contexto

La postulación tiene tres pasos: alta (paso 1), DJ (paso 2) y confirmación con resumen (paso 3). Después, si la beca se otorga, la persona la acepta.

## Decisión

**Paso 1, alta: se delega al legacy.** La API llama a `ORTSecure/Becas/AltaPostulacionBeca` por la API interna ([ADR-003](ADR-003-integracion-servicio-inscripciones-pagos.md)), así que el alta es idéntica por construcción. Las validaciones de `InscriptoPrueba.Valida` siguen en LogicaORT.

- FBCR (44) admite productos de nivel 1, 2 o null. Se cambió en LogicaORT y **aplica a todos los consumidores** (sitio nuevo, sitio viejo, backoffice). Se conserva el código de error 715.
- La regla mira el nivel del producto de la postulación, no otras inscripciones de la persona.
- FBC, FEXA, FBX y FEXAD siguen solo con nivel 1.

**Paso 3, resumen: una sola fuente.** `VD_ADMISIONES_MIS_BECAS` (el DBA le sumó columnas) leída por `ID_INSCRIPTO_PRUEBA` + persona. Viaja como `summary` dentro de la respuesta del `GET` y del `PUT` de la DJ: sin endpoint nuevo, y el front pasa al paso 3 sin otra llamada. Los bloques condicionales los decide `AffidavitFundMatrix`; es la única excepción a "todo campo viaja siempre" del contrato de becas.

**Aceptar beca (`POST scholarships/applications/accept`):** solo graba `ACEPTACION_INSCRIPTO_PRUEBA` si el estado es `A`; no toca el estado. Re-aceptar da **409**, no 200 idempotente: la fecha original no se pisa y el front distingue "ya estaba aceptada".

## Consecuencias

- Las reglas del alta viven en LogicaORT: cambiarlas es un deploy de LogicaORT que afecta a todos los sitios.
- Si `VD_PRUEBAS_DISPONIBLES` ofrece un fondo de nivel 1 para una carrera de nivel 2, el alta lo rechaza con 715: se corrige en la vista o en `Valida`.
- Un doble click en aceptar muestra el 409.
- Falta en la vista: turno, tope del porcentaje, dirección de la localidad y material de estudio.
- Sin comparar con el legacy: las lecturas del paso 1.

## Referencias

- [ADR-024](ADR-024-mis-becas-estado-desde-vista.md) — "Mis becas"
- [ADR-025](ADR-025-becas-inscripcion-obligatoria.md) — inscripción obligatoria
- [ADR-026](ADR-026-becas-dj-por-fondo.md) — DJ por fondo
