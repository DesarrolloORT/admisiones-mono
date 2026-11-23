---
status: accepted
owner: gabriele
updated: 2026-11-11
---

# ADR-049 — El catálogo de mensajes del backend se sincroniza al docs-site del front por `repository_dispatch`

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entrada del 2026-11-11.

## Estado

`accepted` (2026-11-11).

## Contexto

Los mensajes al usuario viven en `.resx` ([ADR-016](ADR-016-mensajes-usuario-resx.md)). El docs-site del front (`docs-site/src/data/ui-catalog.json`) los documenta para Análisis Funcional, y se regeneraba a mano con `npm run docs:catalog`.

## Decisión

- `notify-docs-catalog.yml` (API) hace `repository_dispatch` al front en cada push a `develop` que toque un `.resx`. Se usa el push y no el PR porque un PR puede cambiar o no mergearse, y el docs-site ya toma `develop` como referencia del backend.
- `sync-backend-catalog.yml` (front) regenera el catálogo y lista las claves sin documentar (`list-backend-pending.js`). Si hay cambios, abre o actualiza un PR contra `v1.0.0/main`.
- Las claves nuevas no se agregan solas al CSV: pantalla, estado y condición los escribe una persona.
- Auth: el PAT fine-grained `DOCS_SYNC_TOKEN` va en los dos repos.

## Consecuencias

- Hay 71 claves `.resx` sin documentar (`backend-sin-documentar.txt`).
- Pasar a GitHub App cuando haya quien la cree.

## Referencias

- [ADR-016](ADR-016-mensajes-usuario-resx.md): mensajes en `.resx`.
