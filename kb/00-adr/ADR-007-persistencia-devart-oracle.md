---
status: accepted
owner: rubino-f
updated: 2026-09-17
---

# ADR-007 — Acceso a datos generado por Devart Entity Developer sobre base de datos Oracle preexistente

> **Retrospectivo.** El cambio y sus consecuencias se reconstruyeron del historial; la motivación (por qué Devart) no surgía del código y fue **confirmada por el equipo el 2026-09-17**, elevando la confianza de `medium` a `alta`. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-003--acceso-a-datos-generado-por-devart-entity-developer-sobre-base-de-datos-preexistente-db-first-sin-migraciones-ef-en-el-repo) — candidato `ARCH-HIST-003`.

## Estado

`accepted` (2026-09-17). La motivación quedó confirmada por el equipo el 2026-09-17: ya no es inferencia.

## Contexto

`api-admisiones` arrancó (commit inicial, 2026-03-02) con un modelo de datos ya generado por **Devart Entity Developer** a partir de una base **Oracle** ya existente (confirmado por errores `ORA-00904` corregidos en junio, ver `ARCH-HIST-003`/nota de junio). No hay carpeta de EF Core Migrations ni scripts de esquema versionados en el repo.

## Decisión

Usar Devart Entity Developer como generador *database-first* del modelo de acceso a datos, reverse-engineering desde el esquema Oracle existente, en lugar de Entity Framework Core Code-First con migraciones versionadas en el repositorio.

**Motivación (confirmada por el equipo, 2026-09-17):** la base Oracle ya existía y está compartida con otros sistemas de ORT, por lo que Code-First con migraciones desde este repo no era una opción viable — `api-admisiones` no es dueño del esquema. Devart es la herramienta estándar de la organización para ese escenario database-first.

## Consecuencias

- El esquema de base de datos es una dependencia externa e implícita: los cambios de esquema (agregar una columna, una tabla) se hacen fuera del flujo normal de commits y luego se regenera/ajusta a mano el modelo Devart. Evidencia directa: el commit `ecea9100` (mayo) agrega una columna (`HashTokenPassword`) sin ninguna migración visible.
- **Confirmado en agosto/`ADR-006`**: al menos otros dos sistemas (`FDP`, `LogicaORT`) comparten tablas de esta misma base Oracle (`T_PERSONA`, `T_ENCUESTA_INI`) sin un contrato formal entre ellos — esto ya causó un bug real de inconsistencia de datos (ver `ADR-006`).

## Gaps cerrados (2026-09-17, confirmación del equipo)

- **Por qué Devart:** la base es preexistente y compartida; el repo no es dueño del esquema (ver Motivación).
- **Quién administra el esquema:** el DBA / equipo de infraestructura de ORT, fuera de este repositorio. Los cambios de esquema se solicitan y se aplican por ese canal, no por un commit de `api-admisiones`.

## Gaps abiertos

- Versión concreta del motor Oracle.
- Si existe un versionado formal de cambios de esquema del lado del DBA (y dónde vive), para poder correlacionar un cambio de modelo Devart con el cambio de esquema que lo motivó.
- Falta de análisis funcional documentado sobre las reglas de negocio detrás de las tablas mapeadas.
- Ausencia de contrato formal con los otros sistemas que comparten tablas (`FDP`, `LogicaORT`) — riesgo ya materializado, ver `ADR-006`.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-003`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-003--acceso-a-datos-generado-por-devart-entity-developer-sobre-base-de-datos-preexistente-db-first-sin-migraciones-ef-en-el-repo)
- Relacionado: `ADR-006` (confirma el gap de gobernanza de base de datos con nombres propios: `FDP`, `LogicaORT`)
