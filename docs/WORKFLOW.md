# Workflow

> Tipo: how-to

Este documento centraliza el flujo de trabajo base recomendado para repositorios creados a partir de esta plantilla.

No define un proceso inmutable para todos los proyectos. Cada repositorio derivado debe ajustar ramas, validaciones, despliegues y automatizaciones segun su contexto.

## Flujo de desarrollo diario

1. Crear una rama de trabajo.
2. Implementar cambios en `src/` y sus tests `.spec.ts` co-localizados.
3. Ejecutar validaciones locales:

   ```bash
   npm run lint:check
   npm run test:ci
   npm run build
   npm run test:a11y
   npm run test:e2e:smoke
   ```

4. Hacer commit y abrir un pull request.

## Convenciones de ramas y PR

- La plantilla incluye soporte para ramas `feature/*`, `fix/*`, `hotfix/*` y `dependabot/*` en PRs hacia `main`.
- La plantilla incluye automatizacion para ramas con patron `v*.*.*/main`.
- Si el proyecto conserva ese flujo versionado, los PR a `main` que representen un release deben usar el titulo `release/vX.Y.Z`.
- Si el proyecto conserva ese flujo versionado, la version declarada en `package.json` y `package-lock.json` debe coincidir con la version del release.

## Hooks y validaciones automaticas

- [`.husky/pre-commit`](../.husky/pre-commit) ejecuta:

  ```bash
  node scripts/testing/check-missing-tests.js --staged && npx lint-staged
  ```

- `lint-staged` aplica `eslint --cache --fix .`, `prettier --write .` y `stylelint --fix **/*.scss` segun el tipo de archivo.
- El check de missing tests del hook revisa solo archivos fuente staged.
- `test:a11y` ejecuta Playwright + axe con mocks de API en desktop y mobile.
- `test:e2e:smoke` ejecuta la suite rapida de flujos criticos con mocks.
- `test:e2e:regression` se corre manualmente antes de releases, hotfixes
  delicados o cambios en registro/login/datos personales.

## Estandares de codigo

- Las features deben seguir el flujo `pages/components -> services -> endpoint adapter -> ApiHttpClient -> generated -> API`.
- Solo `features/*/endpoints/*.endpoint.ts` puede importar contratos generados.
- Las pages, components, stores, facades y services no deben importar generated ni `HttpClient` directamente.
- Los servicios son la API interna que consumen los componentes de una feature.
- `ApiHttpClient` es la unica capa que resuelve URLs y usa `environment.API_URL`.
- `ApiHttpClient` cachea por defecto los `GET` sin parámetros; los servicios no deben duplicar ese cache con `shareReplay`.
- Las UIs nuevas o modificadas deben cumplir WCAG 2.2 AA y seguir
  [docs/ACCESSIBILITY.md](./ACCESSIBILITY.md).

Ver [docs/BEST-PRACTICES.md](./BEST-PRACTICES.md).

## GitHub Actions y despliegues

Las automatizaciones listadas abajo son parte de la base de la plantilla. Un proyecto nuevo puede conservarlas, ajustarlas o eliminarlas.

### CI para ramas versionadas

Archivo: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

Se dispara al crear un pull request hacia una rama con patron `v*.*.*/main`.

Trabajos principales:

- `CI Checks` (lint, unit tests, build, accessibility tests y E2E smoke)
- `Coverage`
- `SonarQube Scan`
- notificaciones

Variables y secretos requeridos:

Secretos:

- `RECAPTCHA_KEY`
- `API_URL`
- `SONAR_HOST_URL`
- `SONAR_TOKEN`
- `SONAR_PROJECT_KEY`

Variables:

- `CSP_POLICY`
- `CACHING_ENABLED`

`CSP_POLICY` debe permitir reCAPTCHA v3 cuando el login o endpoints publicos protegidos usen captcha. Como minimo debe incluir `https://www.google.com` y `https://www.gstatic.com` en `script-src`, `https://www.google.com` en `connect-src`, y `https://www.google.com` / `https://recaptcha.google.com` en `frame-src`.

Notas:

- cualquier otro secreto o variable adicional debe configurarse en el repositorio de la misma manera;
- se debe crear el proyecto en SonarQube y contar con el runner correspondiente.

### Despliegue a Produccion

Archivo: [`.github/workflows/cd.yml`](../.github/workflows/cd.yml)

Disparador: ejecucion manual con requisito de contraseña.

Trabajos:

- autenticacion;
- despliegue en `production-server` sobre ambiente `prod`.

Variables requeridas si el proyecto conserva este workflow:

- `AUTH_ADM_SERV`
- `SERVER_SITE_PATH`

### Despliegue a Desarrollo

Archivo: [`.github/workflows/dev-test-deploy.yml`](../.github/workflows/dev-test-deploy.yml)

Disparadores:

- push a ramas `v*.*.*/main`;
- ejecucion manual.

### Despliegue a Preproduccion

Archivo: [`.github/workflows/preprod-test-deploy.yml`](../.github/workflows/preprod-test-deploy.yml)

Disparadores:

- pull requests a `main`;
- ejecucion manual.

### Release

Archivo: [`.github/workflows/release.yml`](../.github/workflows/release.yml)

Disparador: publicacion de release.

Variable requerida si el proyecto conserva este workflow:

- `REPO_NAME`

### Rollback

Archivo: [`.github/workflows/rollback.yml`](../.github/workflows/rollback.yml)

Disparador: ejecucion manual con contraseña.

Usa las mismas variables de despliegue que `cd.yml`.

### Otras automatizaciones

- [`.github/workflows/pr-to-main.yml`](../.github/workflows/pr-to-main.yml): auditoria de dependencias, CI para ramas `feature/*`, `fix/*`, `hotfix/*` y `dependabot/*`, y validacion de version.
- [`.github/workflows/e2e-nightly.yml`](../.github/workflows/e2e-nightly.yml):
  E2E semanal o manual contra preprod controlado cuando `E2E_BASE_URL` esta configurado.
- [`.github/workflows/pr-title-lint.yml`](../.github/workflows/pr-title-lint.yml): exige titulos `release/vX.Y.Z` en PRs a `main`.
- [`.github/workflows/tag-on-push.yml`](../.github/workflows/tag-on-push.yml): genera tags de preproduccion.
- [`.github/workflows/label-manager.yml`](../.github/workflows/label-manager.yml): administra etiquetas del repositorio.
- [`.github/workflows/labeler.yml`](../.github/workflows/labeler.yml): etiqueta PRs automaticamente.
- [`.github/workflows/pr-state-labeler.yml`](../.github/workflows/pr-state-labeler.yml): gestiona etiquetas de estado de PR.
- [`.github/workflows/notification.yml`](../.github/workflows/notification.yml): centraliza notificaciones.
- [`.github/actions/setup-env/action.yml`](../.github/actions/setup-env/action.yml): genera `src/environments/environment.ts` y copia el archivo segun `env-filename`.

## Referencias relacionadas

- [docs/SETUP.md](./SETUP.md)
- [README.md](../README.md)
- [CONTRIBUTING.md](../CONTRIBUTING.md)
