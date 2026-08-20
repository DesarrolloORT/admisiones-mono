# Workflow

> Tipo: how-to

Este documento centraliza el flujo de trabajo base recomendado para repositorios creados a partir de esta plantilla.

No define un proceso inmutable para todos los proyectos. Cada repositorio derivado debe ajustar ramas, validaciones, despliegues y automatizaciones segun su contexto.

## Flujo de desarrollo diario

1. Crear la rama de trabajo con `npm run task:start -- <issue>` (ver [Tareas del project y ramas](#tareas-del-project-y-ramas)).
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

## Tareas del project y ramas

El tablero [Admisiones](https://github.com/orgs/DesarrolloORT/projects/24) es la fuente de verdad del estado de cada tarea. Las tareas de integracion front son issues de `DesarrolloORT/api-admisiones`, mientras que la rama y el pull request viven en este repositorio.

El ciclo esta automatizado en los dos extremos:

1. `npm run task:start -- 137` crea la rama sobre el head de develop, la pushea, mueve la tarea a `In progress Front` y deja el checkout hecho. Acepta `137`, `api-admisiones#137` o la URL del issue.
2. Se trabaja y se abre el pull request hacia `v*.*.*/develop`. No hace falta referenciar el issue a mano: la automatizacion se apoya solo en el nombre de la rama.
3. Al mergear, [`dev-test-deploy.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/dev-test-deploy.yml) despliega a desarrollo y, recien cuando el deploy termino bien, mueve la tarea a `Testing`.

Script: [`scripts/task/task.js`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/scripts/task/task.js). Usa solo built-ins de Node y toma el token de `GITHUB_TOKEN`, con `gh auth token` como fallback local.

### El numero de issue va en el nombre de la rama

`task:start` nombra la rama `v<version>/<tipo>/<issue>-<slug>`, por ejemplo `v1.0.0/feat/137-obtener-las-opciones-del-paso-1`. El tipo sale del prefijo convencional del titulo del issue.

Ese numero al inicio del ultimo tramo no es cosmetico: es el vinculo entre la rama y la tarea, y la unica forma de recuperarla despues del merge. La API de GitHub va de issue a rama (`linkedBranches`) pero no expone el camino inverso, asi que el job de deploy lee el numero del nombre de la rama.

No se usa el "linked branch" nativo de GitHub, el que aparece en la seccion Development del issue. Crearlo requiere `createLinkedBranch`, que modifica el issue y por lo tanto exige permiso de push en `api-admisiones`, donde el equipo de front solo tiene `triage`. Un PAT no puede exceder los permisos de la cuenta que lo emite, asi que no hay token que lo habilite: haria falta que el equipo de backend otorgue write sobre su repositorio, un permiso desproporcionado para ganar un widget en la UI. El vinculo por nombre de rama mas la mencion en el pull request cubren lo mismo en la practica.

Si una rama no abre su ultimo tramo con el numero de issue, el job emite un warning y no mueve nada. El deploy no se ve afectado.

### Token requerido

El movimiento a `Testing` corre dentro de Actions y necesita el secreto de repositorio `PROJECTS_TOKEN`. Debe ser un **fine-grained PAT**, no uno classic, con este minimo:

| Ambito                                  | Permiso                                                                           |
| --------------------------------------- | --------------------------------------------------------------------------------- |
| Resource owner                          | `DesarrolloORT` (no la cuenta personal: hace falta para projects de organizacion) |
| Repository access                       | Solo `admisiones` y `api-admisiones`                                              |
| Organization permissions -> Projects    | Read and write                                                                    |
| Repository permissions -> Issues        | Read-only                                                                         |
| Repository permissions -> Pull requests | Read-only                                                                         |
| Repository permissions -> Contents      | Read-only                                                                         |
| Repository permissions -> Metadata      | Read (obligatorio)                                                                |

Projects tiene que ser read **and** write: el job escribe el campo Status, y con solo lectura puede ver la tarjeta pero no moverla. Sobre el codigo, en cambio, el token nunca escribe: en Actions solo lee el issue en `api-admisiones` y resuelve el pull request desde el sha en `admisiones`.

Un PAT classic no sirve para esto. Ambos repositorios son privados, y en classic la unica forma de leerlos es el scope `repo`, que da control total sobre el codigo incluido push. Los fine-grained separan lectura de escritura; los classic no.

Que el token expire corta la automatizacion en silencio hasta el proximo merge, que falla en rojo en el job `Move task to Testing`. Si la renovacion o la aprobacion del owner se vuelven un tramite recurrente, la alternativa es un GitHub App de la organizacion: mismo minimo de permisos, sin expiracion y sin quedar atado a una persona.

El `GITHUB_TOKEN` por defecto no alcanza: no puede escribir projects a nivel organizacion ni leer issues de `api-admisiones`.

Debe ser un secreto de **repositorio**, no de environment: el job `move_task` no declara `environment`, asi que un secreto de environment resolveria a vacio.

`task:start` no necesita ese secreto: corre local y usa el token del `gh` de cada persona.

## Convenciones de ramas y PR

- La plantilla incluye soporte para ramas `feature/*`, `fix/*`, `hotfix/*` y `dependabot/*` en PRs hacia `main`.
- La plantilla incluye automatizacion para ramas con patron `v*.*.*/main` y `v*.*.*/develop`.
- Las ramas `v*.*.*/develop` son la integracion diaria: sus PRs corren CI permisivo y cada merge despliega a desarrollo.
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

### CI para ramas de desarrollo

Archivo: [`.github/workflows/ci-develop.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/ci-develop.yml)

Se dispara al crear un pull request hacia una rama con patron `v*.*.*/develop`.

Es permisivo por diseno: corre `Prepare API contracts` y `CI Checks` con `strict: false`, es decir solo unit tests (`npm run test:ci`) y build. No corre lint, accessibility tests, E2E smoke ni SonarQube.

El gate completo no desaparece: se aplica cuando la rama develop abre su pull request hacia `v*.*.*/main`, que dispara `ci.yml`.

### Modo estricto de `ci-checks.yml`

Archivo: [`.github/workflows/ci-checks.yml`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.github/workflows/ci-checks.yml)

Workflow reutilizable. El input `strict` (booleano, default `true`) controla los checks pesados:

- `strict: true` (default, usado por `ci.yml` y `pr-to-main.yml`): instala Playwright y corre `lint:check`, `test:ci`, build, `test:a11y` y `test:e2e:smoke`.
- `strict: false` (usado por `ci-develop.yml`): corre solo `test:ci` y build; los demas steps quedan skipped.

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

- push a ramas `v*.*.*/develop`, es decir cada merge de un pull request a develop;
- ejecucion manual sobre cualquier rama.

Despliega en el runner `funcionarios-desa` sobre ambiente `desarrollo`. Usa `concurrency: deploy-desa` con `cancel-in-progress`, porque el recurso compartido es el sitio IIS y no la rama: si se mergean dos pull requests seguidos, el deploy viejo se cancela y gana el estado mas nuevo.

Variable requerida:

- `DEV_SERVER_SITE_PATH` (en el environment `desarrollo`). El workflow aborta si esta vacia o si el path no existe en el runner, para no borrar la raiz del disco.

Secreto requerido:

- `PROJECTS_TOKEN`, un fine-grained PAT con Projects read/write y lectura de los dos repositorios. Lo usa el job `Move task to Testing`, que corre despues del deploy y mueve la tarea del project a `Testing`. Ver [Tareas del project y ramas](#tareas-del-project-y-ramas).

El job solo corre en merges (`github.event_name == 'push'`) y solo si el deploy salio bien: un deploy manual no cambia el estado de ninguna tarea. Va en un job aparte del deploy, asi que si el secreto falta, desa igual queda actualizado y el fallo aparece acotado a ese job.

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
