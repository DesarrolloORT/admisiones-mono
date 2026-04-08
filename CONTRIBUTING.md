# Contributing

> Tipo: how-to

Guia minima para contribuir cambios a esta plantilla y para adaptar correctamente un repositorio recien creado a partir de ella.

## Antes de empezar

- Seguir la puesta en marcha de [docs/SETUP.md](docs/SETUP.md).
- Trabajar sobre una rama dedicada.
- Mantener los tests en `tests/` replicando la estructura de `src/app/`.
- Si el trabajo ocurre en un repositorio derivado, ajustar estas reglas al contexto real del proyecto.

## Validaciones locales

Ejecutar antes de abrir un pull request:

```bash
npm run lint:check
npm run test
npm run build
```

Si corresponde actualizar snapshots:

```bash
npm run test:snapshot
```

## Commits y pre-commit

El hook [`.husky/pre-commit`](.husky/pre-commit) ejecuta:

```bash
node scripts/testing/check-missing-tests.js && npx lint-staged
```

Eso implica que:

- no deberian subirse cambios con formato roto;
- no deberian agregarse archivos fuente en `src/app/` sin su cobertura minima esperada en `tests/`.

## Pull requests

- Si el proyecto conserva el flujo base, usar ramas `feature/*`, `fix/*`, `hotfix/*` o `dependabot/*` para trabajo regular hacia `main`.
- Si el proyecto conserva el flujo versionado, seguir lo documentado en [docs/WORKFLOW.md](docs/WORKFLOW.md).
- Si el proyecto conserva releases via PR a `main`, el titulo debe respetar `release/vX.Y.Z`.

## Documentacion requerida

Cada cambio debe clasificar su impacto documental:

- `docs-none`
- `docs-light`
- `docs-required`
- `docs-technical-decision`

Reglas:

- actualizar la documentacion junto con el cambio;
- usar nombres exactos de rutas, scripts, comandos, archivos y contratos;
- evitar duplicacion y contenido obsoleto;
- si se usa una exencion documental, dejar el motivo visible.

## Referencias

- [README.md](README.md)
- [docs/SETUP.md](docs/SETUP.md)
- [docs/WORKFLOW.md](docs/WORKFLOW.md)
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/BEST-PRACTICES.md](docs/BEST-PRACTICES.md)
