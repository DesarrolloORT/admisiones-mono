---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-007 — Acceso a datos generado por Devart Entity Developer sobre base de datos Oracle preexistente

> **Retrospectivo, confianza `medium`.** A diferencia de otros ADR de esta serie, la motivación (por qué Devart y no otra alternativa) no está confirmada — solo el cambio y sus consecuencias. **No promover a `accepted` sin validación explícita del equipo de datos/backend.** Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-003--acceso-a-datos-generado-por-devart-entity-developer-sobre-base-de-datos-preexistente-db-first-sin-migraciones-ef-en-el-repo) — candidato `ARCH-HIST-003`.

## Estado

`draft` — pendiente de validación. Confianza `medium`: el qué y las consecuencias están claros; el porqué es inferido, no confirmado.

## Contexto

`api-admisiones` arrancó (commit inicial, 2026-03-02) con un modelo de datos ya generado por **Devart Entity Developer** a partir de una base **Oracle** ya existente (confirmado por errores `ORA-00904` corregidos en junio, ver `ARCH-HIST-003`/nota de junio). No hay carpeta de EF Core Migrations ni scripts de esquema versionados en el repo.

## Decisión

Usar Devart Entity Developer como generador *database-first* del modelo de acceso a datos, reverse-engineering desde el esquema Oracle existente, en lugar de Entity Framework Core Code-First con migraciones versionadas en el repositorio.

## Consecuencias

- El esquema de base de datos es una dependencia externa e implícita: los cambios de esquema (agregar una columna, una tabla) se hacen fuera del flujo normal de commits y luego se regenera/ajusta a mano el modelo Devart. Evidencia directa: el commit `ecea9100` (mayo) agrega una columna (`HashTokenPassword`) sin ninguna migración visible.
- **Confirmado en agosto/`ADR-006`**: al menos otros dos sistemas (`FDP`, `LogicaORT`) comparten tablas de esta misma base Oracle (`T_PERSONA`, `T_ENCUESTA_INI`) sin un contrato formal entre ellos — esto ya causó un bug real de inconsistencia de datos (ver `ADR-006`).

## Gaps

- **Falta contexto de base de datos**: motor Oracle confirmado, pero no su versión, quién administra el esquema formalmente, ni si existe un proceso de versionado de cambios de esquema fuera de este repo.
- Falta de análisis funcional documentado sobre las reglas de negocio detrás de las tablas mapeadas.
- No se confirmó por qué se eligió Devart sobre otras alternativas (Code-First, Dapper, otro ORM).

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-003`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-003--acceso-a-datos-generado-por-devart-entity-developer-sobre-base-de-datos-preexistente-db-first-sin-migraciones-ef-en-el-repo)
- Relacionado: `ADR-006` (confirma el gap de gobernanza de base de datos con nombres propios: `FDP`, `LogicaORT`)
