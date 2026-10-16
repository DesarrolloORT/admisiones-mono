---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-026 — Becas: la DJ pregunta según el fondo y lo que no se pregunta queda en NULL

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-09-23, 2026-09-28, 2026-09-29 y 2026-10-06 15:00.

## Estado

`accepted` (2026-09-23).

## Contexto

La declaración jurada (DJ) se rediseñó: cada fondo (26 FBC, 33 FEXA, 36 FCL, 44 FBCR, 57 FEXAD) pide secciones distintas, y negocio sacó o agregó preguntas sobre las columnas que ya usaba el legacy en `T_DECLARACION_JURADA_WEB`.

## Decisión

- **`AffidavitFundMatrix` decide qué pregunta cada fondo.** El guardado, la completitud del confirm y el resumen del paso 3 salen de la misma matriz, no del dato.
- **Campo no preguntado → NULL**, y el guardado limpia los borradores que ya lo tenían. Se relee la columna, no el request. Casos:
  - Secundaria (`CODIGO_INSTITUCION_BAC`, `ULTIMOANIO_SECUNDARIA_APR_DJ`): NULL en todos los fondos.
  - FCL con trabaja = SÍ: `EXPERIENCIA_LABORAL_DJ` en NULL, aunque obviamente tenga experiencia.
  - Otros estudios = NO: se limpia el detalle.
  - FEXA (33): sin integrantes ni gastos aunque vengan en el request.
- **No se derivan de otras tablas.** Se evaluó derivar la secundaria de la encuesta inicial y de `T_BACHILLERATO_PERSONA` (99,96 % de cobertura) y negocio prefirió no cargarla.
- **Columnas del legacy antes que columnas nuevas:** "¿Trabajás actualmente?", lugar, cargo y otros estudios reusan las del legacy (Devart ya las mapeaba).
- **Largos validados en el guardado**, para que no salte el error en Oracle.
- Breaking changes de contrato aceptados mientras la DJ no esté en producción.

## Consecuencias

- La cascada de antecedentes laborales de FCL queda invertida respecto del legacy (la raíz es si trabaja).
- Los borradores FCL existentes pierden `EXPERIENCIA_LABORAL_DJ` en el próximo guardado.
- `ConfirmAffidavit` copia `CODIGO_INSTITUCION_BAC` a `T_INSCRIPTO_PRUEBA` con `??`: con NULL conserva el valor del inscripto.
- Un front viejo que mande campos eliminados no da error: se ignoran.

## Referencias

- `api-admisiones/docs/BECAS-DECLARACION-JURADA.md`
- [ADR-021](ADR-021-becas-paridad-escrituras-legacy.md) — paridad con el legacy
