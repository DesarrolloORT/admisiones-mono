---
status: accepted
owner: rubino-f
updated: 2026-09-15
---

# ADR-008 — Frontend `admisiones` bootstrapped desde un starter interno `angular-template`

> **Retrospectivo.** Motivación confirmada por testimonio directo del autor (`rubino-f`) el 2026-09-15, no solo inferencia por patrón de commits. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-006--frontend-admisiones-bootstrapped-desde-un-starter-interno-angular-template) — candidato `ARCH-HIST-006`.

## Estado

`accepted` (2026-09-15).

## Contexto

El frontend `admisiones` se creó el 2026-04-08 con scaffolding completo (devcontainer, CI/CD, labeler, issue templates) ya resuelto, no un `ng new` vacío.

## Decisión

Bootstrapear el frontend desde `angular-template`, **el starter oficial de ORT para arrancar repos** (confirmado por el autor): un repositorio de la organización con el que empiezan todos los proyectos frontend, actualizado constantemente para futuros inicios de proyecto. Confirmado en el código por el commit `5df9ec37` ("Rename project from 'angular-template' to 'admisiones' in configuration files"), 11 minutos después del commit inicial.

## Motivación

Reusar convenciones ya resueltas (CI/CD, labeler, estructura) y mantenerse alineado con el estándar de arranque de proyectos de ORT, en vez de reconstruir ese scaffolding por proyecto. Mismo patrón organizacional que el backend (`ADR-002`, plantilla `NewApi`): ORT mantiene starters propios por stack.

## Consecuencias

Convenciones de CI/CD, labeler y estructura de proyecto ya resueltas desde el día uno. Al ser un starter mantenido activamente, futuras mejoras a `angular-template` no se propagan automáticamente a `admisiones` una vez bifurcado — quedan a criterio del equipo si portarlas manualmente.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-006`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-006--frontend-admisiones-bootstrapped-desde-un-starter-interno-angular-template)
- Relacionado: `ADR-002` (mismo patrón en el backend, con `NewApi`)
