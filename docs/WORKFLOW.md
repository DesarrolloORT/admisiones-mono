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

- [`.husky/pre-commit`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.husky/pre-commit) ejecuta:

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
- Solo `features/*/api/*.endpoint.ts` puede importar contratos generados.
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

Archivo: [`.github/workflows/ci.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/ci.yml)

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

- `CSP_POLICY_TEMPLATE`
- `RECAPTCHA_NONCE`
- `CACHING_ENABLED`

`CSP_POLICY_TEMPLATE` debe permitir reCAPTCHA v3 cuando el login o endpoints publicos protegidos usen captcha. Como minimo debe incluir `https://www.google.com` y `https://www.gstatic.com` en `script-src`, `https://www.google.com` en `connect-src`, y `https://www.google.com` / `https://recaptcha.google.com` en `frame-src`. Acepta los placeholders `{{API_URL}}` y `{{FDP_API_URL}}`, que `@desarrolloort/azure-env-sync` resuelve antes de generar `CSP_POLICY`.

`script-src` no debe usar `unsafe-inline` ni hashes fijos para permitir los scripts inline que inyecta `api.js` de Google. En su lugar, `script-src` debe incluir `'nonce-<RECAPTCHA_NONCE>' 'strict-dynamic'`, y `RECAPTCHA_NONCE` debe ser exactamente el mismo valor en ambas variables. Google propaga ese nonce a los scripts inline que agrega, y `strict-dynamic` habilita cualquier script cargado por uno con nonce valido sin depender de hashes que se rompen cuando Google cambia el contenido del script sin aviso.

`img-src` debe incluir `blob:` ademas de `'self' data:`. El preview/compresion de imagenes (`ImageCompressionUtils` de `@desarrolloort/ngx-utils`, usado en la subida de identidad y OCR de registro) genera URLs `blob:` con `URL.createObjectURL`; sin `blob:` en `img-src`, el navegador bloquea esas imagenes aunque la compresion y subida sigan funcionando.

Notas:

- cualquier otro secreto o variable adicional debe configurarse en el repositorio de la misma manera;
- se debe crear el proyecto en SonarQube y contar con el runner correspondiente.

### Despliegue a Produccion

Archivo: [`.github/workflows/cd.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/cd.yml)

Disparador: ejecucion manual con requisito de contraseña.

Trabajos:

- autenticacion;
- despliegue en `production-server` sobre ambiente `prod`.

Variables requeridas si el proyecto conserva este workflow:

- `AUTH_ADM_SERV`
- `SERVER_SITE_PATH`

### Despliegue a Desarrollo

Archivo: [`.github/workflows/dev-test-deploy.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/dev-test-deploy.yml)

Disparadores:

- push a ramas `v*.*.*/main`;
- ejecucion manual.

### Despliegue a Preproduccion

Archivo: [`.github/workflows/preprod-test-deploy.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/preprod-test-deploy.yml)

Disparadores:

- pull requests a `main`;
- ejecucion manual.

### Release

Archivo: [`.github/workflows/release.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/release.yml)

Disparador: publicacion de release.

Variable requerida si el proyecto conserva este workflow:

- `REPO_NAME`

### Rollback

Archivo: [`.github/workflows/rollback.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/rollback.yml)

Disparador: ejecucion manual con contraseña.

Usa las mismas variables de despliegue que `cd.yml`.

### Otras automatizaciones

- [`.github/workflows/pr-to-main.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/pr-to-main.yml): auditoria de dependencias, CI para ramas `feature/*`, `fix/*`, `hotfix/*` y `dependabot/*`, y validacion de version.
- [`.github/workflows/pr-title-lint.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/pr-title-lint.yml): exige titulos `release/vX.Y.Z` en PRs a `main`.
- [`.github/workflows/tag-on-push.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/tag-on-push.yml): genera tags de preproduccion.
- [`.github/workflows/label-manager.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/label-manager.yml): administra etiquetas del repositorio.
- [`.github/workflows/labeler.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/labeler.yml): etiqueta PRs automaticamente.
- [`.github/workflows/pr-state-labeler.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/pr-state-labeler.yml): gestiona etiquetas de estado de PR.
- [`.github/workflows/notification.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/notification.yml): centraliza notificaciones.

## Referencias relacionadas

- [docs/SETUP.md](./SETUP.md)
- [README.md](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/README.md)
- [CONTRIBUTING.md](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/CONTRIBUTING.md)
