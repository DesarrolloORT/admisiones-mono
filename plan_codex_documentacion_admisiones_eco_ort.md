# Plan de implementación — Documentación Docusaurus de Admisiones integrada con ECO-ORT

## Rol del agente

Trabajá sobre el repositorio:

```text
DesarrolloORT/admisiones
```

La documentación Docusaurus ya existe dentro del repositorio y actualmente se encuentra en desarrollo sobre la rama:

```text
v1.0.0/main
```

Esta rama debe ser la fuente de la documentación publicada de Admisiones hasta que el sistema salga a producción.

A futuro, la rama publicada será:

```text
main
```

La solución debe permitir cambiar la rama publicada mediante configuración, sin volver a modificar el workflow.

---

# Objetivo

Implementar un flujo de publicación de documentación con estas características:

1. La documentación permanece dentro del repositorio de Admisiones.
2. Docusaurus se compila desde la rama configurada como rama publicable.
3. El sitio estático se despliega en IIS en una carpeta exclusiva para Admisiones.
4. ECO-ORT no contiene ni copia el proyecto Docusaurus.
5. ECO-ORT solamente enlazará a la URL publicada.
6. El despliegue de Admisiones no puede eliminar ni sobrescribir la documentación de otros sistemas.
7. No utilizar `iframe`.
8. El cambio futuro de `v1.0.0/main` a `main` debe realizarse cambiando una variable de GitHub, sin modificar código.

---

# Estado actual relevante

Verificar antes de realizar cambios:

```text
docs/
docs-site/
.github/workflows/docs.yml
```

El workflow actual:

- construye `docs-site`;
- genera `docs-site/build`;
- sube el build como artifact;
- utiliza el runner `development-server`;
- copia el artifact a IIS;
- elimina previamente todo el contenido de `DOCS_SERVER_SITE_PATH`.

La configuración actual de Docusaurus utiliza aproximadamente:

```javascript
baseUrl: '/';
```

y:

```javascript
routeBasePath: 'proyectos/admisiones';
```

La nueva publicación debe quedar disponible en:

```text
https://<dominio-eco-ort>/docs/admisiones/
```

No asumir el dominio definitivo. Debe configurarse mediante variables.

---

# Alcance

## Incluir

- Ajustes en `docs-site/docusaurus.config.mjs`.
- Ajustes en `.github/workflows/docs.yml`.
- Validaciones de seguridad del directorio de despliegue.
- Variables de configuración documentadas.
- Validación de build para pull requests.
- Despliegue solamente desde la rama configurada.
- Documentación operativa del flujo.
- Un archivo de metadata de publicación dentro del build.
- Pruebas locales o verificaciones automatizadas razonables.

## No incluir

- Cambios en el repositorio ECO-ORT.
- Embebido mediante `iframe`.
- Copia del código fuente de Docusaurus dentro de ECO-ORT.
- Submodules o subtrees Git.
- Modificaciones funcionales del frontend Angular de Admisiones.
- Cambios en la API de Admisiones.
- Rediseño general de la documentación.
- Cambios innecesarios en archivos Markdown existentes.

---

# Variables de GitHub requeridas

Configurar o documentar las siguientes variables de repositorio o del environment `desarrollo`:

```text
DOCS_PUBLISH_BRANCH=v1.0.0/main
DOCS_SYSTEM_SLUG=admisiones
DOCS_SERVER_ROOT=D:\Sites\EcoDocs
DOCS_SITE_ORIGIN=https://<dominio-eco-ort>
DOCS_BASE_URL=/docs/admisiones/
DOCS_SOURCE_BRANCH=v1.0.0/main
```

## Significado

### `DOCS_PUBLISH_BRANCH`

Rama autorizada para publicar documentación.

Valor inicial:

```text
v1.0.0/main
```

Valor futuro:

```text
main
```

### `DOCS_SYSTEM_SLUG`

Nombre estable y seguro del sistema:

```text
admisiones
```

Debe utilizarse para calcular la carpeta de publicación.

### `DOCS_SERVER_ROOT`

Directorio raíz compartido donde se alojarán las documentaciones de todos los sistemas.

Ejemplo:

```text
D:\Sites\EcoDocs
```

El workflow no debe borrar ni reemplazar este directorio.

### `DOCS_SITE_ORIGIN`

Origen del sitio sin path final.

Ejemplo:

```text
https://eco-ort.ort.edu.uy
```

### `DOCS_BASE_URL`

Ruta pública exclusiva del sistema.

```text
/docs/admisiones/
```

Debe comenzar y terminar con `/`.

### `DOCS_SOURCE_BRANCH`

Rama usada para construir enlaces de edición hacia GitHub.

Valor inicial:

```text
v1.0.0/main
```

A futuro:

```text
main
```

---

# Implementación paso a paso

## Paso 1 — Revisar el estado actual

Antes de cambiar archivos:

1. Confirmar que existe `docs-site/package-lock.json`.
2. Confirmar que `npm ci --prefix docs-site` funciona.
3. Confirmar que `npm run build --prefix docs-site` funciona.
4. Revisar:
   - `docs-site/docusaurus.config.mjs`;
   - `docs-site/sidebars.js`;
   - `docs-site/static/web.config`;
   - `.github/workflows/docs.yml`.
5. No modificar contenido funcional de la documentación salvo que sea necesario para corregir enlaces rotos provocados por el nuevo `baseUrl`.

Registrar en el resumen final cualquier problema previo encontrado.

---

## Paso 2 — Parametrizar Docusaurus

Modificar:

```text
docs-site/docusaurus.config.mjs
```

La configuración debe leer variables de entorno.

Implementar una estructura equivalente a:

```javascript
import { themes as prismThemes } from 'prism-react-renderer';

const siteOrigin = process.env.DOCS_SITE_ORIGIN ?? 'http://localhost';
const baseUrl = process.env.DOCS_BASE_URL ?? '/docs/admisiones/';
const sourceBranch = process.env.DOCS_SOURCE_BRANCH ?? 'v1.0.0/main';
const encodedSourceBranch = encodeURIComponent(sourceBranch);

if (!baseUrl.startsWith('/') || !baseUrl.endsWith('/')) {
  throw new Error('DOCS_BASE_URL must start and end with "/".');
}

const config = {
  title: 'DesarrolloORT Frontend Docs',
  tagline: 'Documentación técnica y funcional de los proyectos frontend',
  url: siteOrigin,
  baseUrl,
  trailingSlash: true,
  onBrokenLinks: 'throw',

  presets: [
    [
      'classic',
      {
        blog: false,
        docs: {
          path: '../docs',
          routeBasePath: '/',
          include: ['*.md'],
          sidebarPath: './sidebars.js',
          editUrl: `https://github.com/DesarrolloORT/admisiones/edit/${encodedSourceBranch}/`,
        },
        sitemap: false,
      },
    ],
  ],

  // Mantener las configuraciones actuales que no entren en conflicto.
};

export default config;
```

## Requisitos

- Mantener Mermaid.
- Mantener configuración de idioma español.
- Mantener `noindex`.
- Mantener tema, navbar, footer y Prism salvo cambios indispensables.
- Cambiar `onBrokenLinks` de `warn` a `throw` para impedir publicar documentación rota.
- Mantener `trailingSlash: true`.
- No duplicar la ruta `proyectos/admisiones` dentro de Docusaurus.
- La ruta raíz del Docusaurus debe ser relativa a:

```text
/docs/admisiones/
```

Resultado esperado:

```text
https://<dominio>/docs/admisiones/
https://<dominio>/docs/admisiones/arquitectura/
https://<dominio>/docs/admisiones/convenciones/
```

---

## Paso 3 — Verificar el `web.config`

Revisar:

```text
docs-site/static/web.config
```

Mantener:

- Windows Authentication, si sigue siendo el mecanismo definido.
- `X-Frame-Options: DENY`.
- `frame-ancestors 'none'`.
- `X-Robots-Tag: noindex, nofollow, noarchive`.
- CSP restrictiva.

No habilitar `iframe`.

Verificar que los archivos estáticos y las rutas con `trailingSlash` funcionen correctamente en IIS.

No ampliar la CSP salvo que el build o Mermaid lo requieran de forma demostrable.

---

## Paso 4 — Generar metadata de publicación

Crear:

```text
docs-site/scripts/generate-deployment-metadata.mjs
```

El script debe generar:

```text
docs-site/static/deployment.json
```

Contenido esperado:

```json
{
  "system": "admisiones",
  "repository": "DesarrolloORT/admisiones",
  "branch": "v1.0.0/main",
  "commitSha": "<sha>",
  "publishedAt": "<fecha ISO UTC>",
  "docsUrl": "https://<dominio>/docs/admisiones/"
}
```

Los valores deben provenir de variables de entorno:

```text
DOCS_SYSTEM_SLUG
GITHUB_REPOSITORY
GITHUB_REF_NAME
GITHUB_SHA
DOCS_SITE_ORIGIN
DOCS_BASE_URL
```

Agregar un script en:

```text
docs-site/package.json
```

Por ejemplo:

```json
{
  "scripts": {
    "generate:deployment-metadata": "node scripts/generate-deployment-metadata.mjs",
    "build": "npm run generate:deployment-metadata && docusaurus build"
  }
}
```

## Requisitos

- No incluir secretos.
- El JSON debe ser válido.
- El archivo debe quedar accesible luego del despliegue en:

```text
/docs/admisiones/deployment.json
```

- Para builds locales, utilizar valores razonables como `local` cuando no existan variables de GitHub.

---

## Paso 5 — Ajustar los eventos del workflow

Modificar:

```text
.github/workflows/docs.yml
```

El workflow debe ejecutarse en:

### Pull requests

Validar documentación en pull requests dirigidos a:

```yaml
branches:
  - main
  - 'v*.*.*/main'
```

Aplicar filtros por paths:

```yaml
paths:
  - 'docs-site/**'
  - 'docs/**'
  - 'README.md'
  - 'CONTRIBUTING.md'
  - 'src/app/features/**/*.md'
  - '.github/workflows/docs.yml'
```

### Push

Escuchar pushes sobre:

```yaml
branches:
  - main
  - 'v*.*.*/main'
```

El workflow puede construir en ambas clases de rama, pero solamente debe desplegar cuando:

```text
github.ref_name == vars.DOCS_PUBLISH_BRANCH
```

### Ejecución manual

Mantener:

```yaml
workflow_dispatch:
```

La ejecución manual también debe validar la rama.

No permitir que un `workflow_dispatch` desde una rama diferente publique accidentalmente, salvo que se agregue un input explícito y seguro. Preferir bloquear el deploy si la rama ejecutada no coincide con `DOCS_PUBLISH_BRANCH`.

---

## Paso 6 — Mantener un job de build independiente

El job `build` debe:

1. Ejecutarse en `ubuntu-latest`.
2. Hacer checkout de la rama o commit actual.
3. Instalar Node 20.
4. Ejecutar:

```bash
npm ci --prefix docs-site
```

5. Ejecutar:

```bash
npm run build --prefix docs-site
```

6. Pasar las variables:

```yaml
env:
  DOCS_SITE_ORIGIN: ${{ vars.DOCS_SITE_ORIGIN }}
  DOCS_BASE_URL: ${{ vars.DOCS_BASE_URL }}
  DOCS_SOURCE_BRANCH: ${{ vars.DOCS_SOURCE_BRANCH }}
  DOCS_SYSTEM_SLUG: ${{ vars.DOCS_SYSTEM_SLUG }}
```

7. Subir el artifact con un nombre específico:

```text
docs-admisiones-<sha>
```

o equivalente.

Ejemplo:

```yaml
- name: Upload documentation artifact
  uses: actions/upload-artifact@v7
  with:
    name: docs-${{ vars.DOCS_SYSTEM_SLUG }}-${{ github.sha }}
    path: docs-site/build
    if-no-files-found: error
    retention-days: 7
```

---

## Paso 7 — Restringir el job de despliegue

El job `deploy` debe cumplir todas estas condiciones:

```yaml
if: >
  github.event_name != 'pull_request' &&
  github.ref_name == vars.DOCS_PUBLISH_BRANCH
```

Debe:

- depender de `build`;
- utilizar `runs-on: development-server`;
- utilizar el environment `desarrollo`;
- descargar exactamente el artifact generado por el job `build`;
- publicar únicamente la carpeta de Admisiones.

No desplegar desde pull requests.

No desplegar desde `main` mientras:

```text
DOCS_PUBLISH_BRANCH=v1.0.0/main
```

---

## Paso 8 — Calcular de forma segura el directorio de destino

No utilizar una variable que apunte directamente al directorio final sin validación.

Calcular:

```text
target = DOCS_SERVER_ROOT + DOCS_SYSTEM_SLUG
```

Ejemplo:

```text
D:\Sites\EcoDocs\admisiones
```

Implementar validaciones equivalentes a:

```powershell
$root = [System.IO.Path]::GetFullPath('${{ vars.DOCS_SERVER_ROOT }}')
$slug = '${{ vars.DOCS_SYSTEM_SLUG }}'

if ([string]::IsNullOrWhiteSpace($root)) {
  throw 'DOCS_SERVER_ROOT is required.'
}

if ($slug -notmatch '^[a-z0-9-]+$') {
  throw 'DOCS_SYSTEM_SLUG contains invalid characters.'
}

$target = [System.IO.Path]::GetFullPath((Join-Path $root $slug))
$rootWithSeparator = $root.TrimEnd('\') + '\'

if (-not $target.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
  throw 'Target directory is outside DOCS_SERVER_ROOT.'
}

if ($target.TrimEnd('\') -eq $root.TrimEnd('\')) {
  throw 'Target directory cannot be DOCS_SERVER_ROOT.'
}
```

## Requisito crítico

Nunca ejecutar:

```powershell
Remove-Item
```

sobre:

```text
DOCS_SERVER_ROOT
```

Solamente puede limpiarse:

```text
DOCS_SERVER_ROOT\admisiones
```

---

## Paso 9 — Publicar mediante staging

Evitar copiar directamente sobre el directorio activo.

Usar:

```text
D:\Sites\EcoDocs\admisiones.__staging
D:\Sites\EcoDocs\admisiones
D:\Sites\EcoDocs\admisiones.__backup
```

Flujo recomendado:

1. Eliminar el staging previo si existe.
2. Crear el staging.
3. Copiar el artifact completo al staging.
4. Verificar que exista:
   - `index.html`;
   - `deployment.json`;
   - `web.config`.
5. Eliminar backup previo.
6. Renombrar el target actual a backup si existe.
7. Renombrar staging a target.
8. Si falla el cambio:
   - restaurar backup;
   - marcar el workflow como fallido.
9. Eliminar backup solamente después de una publicación exitosa.

Implementar manejo de errores con `try/catch`.

No dejar el sitio sin contenido ante un error de copia.

---

## Paso 10 — Verificar la publicación

Después de publicar, realizar verificaciones locales sobre el filesystem:

```powershell
$requiredFiles = @(
  'index.html',
  'deployment.json',
  'web.config'
)
```

Fallar si falta alguno.

Cuando el runner tenga acceso HTTP al sitio, agregar una verificación opcional:

```powershell
Invoke-WebRequest `
  -Uri "${{ vars.DOCS_SITE_ORIGIN }}${{ vars.DOCS_BASE_URL }}deployment.json" `
  -UseBasicParsing
```

No bloquear la primera implementación si Windows Authentication impide validar HTTP desde el runner. En ese caso, documentar la limitación y mantener la validación de filesystem.

---

## Paso 11 — Documentar la operación

Crear o actualizar:

```text
docs-site/README.md
```

Incluir:

1. Cómo ejecutar localmente.
2. Variables utilizadas.
3. Rama publicada actual.
4. Ruta pública.
5. Directorio físico esperado.
6. Cómo cambiar la publicación de `v1.0.0/main` a `main`.
7. Cómo ejecutar manualmente el workflow.
8. Cómo verificar `deployment.json`.
9. Cómo hacer rollback manual utilizando la carpeta backup o un artifact previo.
10. Advertencia explícita de que ECO-ORT solo enlaza al sitio.

Agregar una sección:

```markdown
## Migración futura a main

Cuando Admisiones pase a producción:

1. Confirmar que `main` contiene la documentación vigente.
2. Cambiar `DOCS_PUBLISH_BRANCH` a `main`.
3. Cambiar `DOCS_SOURCE_BRANCH` a `main`.
4. Ejecutar manualmente el workflow desde `main`.
5. Verificar `deployment.json`.
6. No modificar el workflow.
```

---

# Propuesta de workflow esperada

El resultado debe seguir aproximadamente esta estructura:

```yaml
name: Frontend documentation

on:
  pull_request:
    branches:
      - main
      - 'v*.*.*/main'
    paths:
      - 'docs-site/**'
      - 'docs/**'
      - 'README.md'
      - 'CONTRIBUTING.md'
      - 'src/app/features/**/*.md'
      - '.github/workflows/docs.yml'

  push:
    branches:
      - main
      - 'v*.*.*/main'
    paths:
      - 'docs-site/**'
      - 'docs/**'
      - 'README.md'
      - 'CONTRIBUTING.md'
      - 'src/app/features/**/*.md'
      - '.github/workflows/docs.yml'

  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: frontend-docs-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    name: Build documentation
    runs-on: ubuntu-latest

    env:
      DOCS_SITE_ORIGIN: ${{ vars.DOCS_SITE_ORIGIN }}
      DOCS_BASE_URL: ${{ vars.DOCS_BASE_URL }}
      DOCS_SOURCE_BRANCH: ${{ vars.DOCS_SOURCE_BRANCH }}
      DOCS_SYSTEM_SLUG: ${{ vars.DOCS_SYSTEM_SLUG }}

    steps:
      - name: Checkout repository
        uses: actions/checkout@v6

      - name: Setup Node.js
        uses: actions/setup-node@v6
        with:
          node-version: '20'
          cache: npm
          cache-dependency-path: docs-site/package-lock.json

      - name: Install dependencies
        run: npm ci --prefix docs-site

      - name: Build documentation
        run: npm run build --prefix docs-site

      - name: Upload documentation artifact
        uses: actions/upload-artifact@v7
        with:
          name: docs-${{ vars.DOCS_SYSTEM_SLUG }}-${{ github.sha }}
          path: docs-site/build
          if-no-files-found: error
          retention-days: 7

  deploy:
    name: Deploy documentation
    if: >
      github.event_name != 'pull_request' &&
      github.ref_name == vars.DOCS_PUBLISH_BRANCH
    needs: build
    runs-on: development-server
    environment: desarrollo

    steps:
      - name: Download documentation artifact
        uses: actions/download-artifact@v8
        with:
          name: docs-${{ vars.DOCS_SYSTEM_SLUG }}-${{ github.sha }}
          path: docs-site-build

      - name: Publish documentation safely
        shell: pwsh
        run: |
          # Implementar validación de root, slug, target,
          # staging, backup, rollback y validación de archivos.
```

No copiar literalmente sin revisar sintaxis y comportamiento en GitHub Actions.

---

# Validaciones requeridas

## Locales

Ejecutar:

```bash
npm ci --prefix docs-site
npm run build --prefix docs-site
```

Verificar que el build contenga:

```text
docs-site/build/index.html
docs-site/build/deployment.json
docs-site/build/web.config
```

Verificar que los enlaces generados utilicen:

```text
/docs/admisiones/
```

y no:

```text
/
```

ni:

```text
/proyectos/admisiones/
```

---

## Workflow

Comprobar conceptualmente o mediante una rama de prueba:

### Caso 1

```text
Pull request hacia v1.0.0/main
```

Resultado:

```text
build: sí
deploy: no
```

### Caso 2

```text
Push a v1.0.0/main
DOCS_PUBLISH_BRANCH=v1.0.0/main
```

Resultado:

```text
build: sí
deploy: sí
```

### Caso 3

```text
Push a main
DOCS_PUBLISH_BRANCH=v1.0.0/main
```

Resultado:

```text
build: sí
deploy: no
```

### Caso 4

```text
Push a feature/*
```

Resultado:

```text
workflow automático: no
```

salvo que forme parte de un pull request hacia una rama observada.

### Caso 5

Luego de cambiar:

```text
DOCS_PUBLISH_BRANCH=main
DOCS_SOURCE_BRANCH=main
```

Un push a `main` debe publicar sin modificar el workflow.

---

# Criterios de aceptación

La implementación se considera completa cuando:

- [ ] Docusaurus compila localmente.
- [ ] `baseUrl` es configurable.
- [ ] La ruta pública es `/docs/admisiones/`.
- [ ] `routeBasePath` no duplica el nombre del sistema.
- [ ] Los enlaces internos funcionan bajo una subruta.
- [ ] Los enlaces de edición apuntan a la rama fuente configurada.
- [ ] Los pull requests validan el build sin desplegar.
- [ ] `v1.0.0/main` es la única rama que despliega inicialmente.
- [ ] `main` construye, pero no despliega mientras no sea la rama configurada.
- [ ] Cambiar a `main` requiere solamente modificar variables.
- [ ] El workflow nunca elimina `DOCS_SERVER_ROOT`.
- [ ] El workflow solamente modifica la carpeta `admisiones`.
- [ ] Existe staging y rollback ante fallos.
- [ ] `deployment.json` se publica correctamente.
- [ ] Se conserva Windows Authentication.
- [ ] Se mantiene bloqueado el uso mediante `iframe`.
- [ ] No se realizan cambios en ECO-ORT.
- [ ] Existe documentación operativa del proceso.

---

# Entregables esperados

1. Cambios en:

```text
docs-site/docusaurus.config.mjs
```

2. Cambios en:

```text
.github/workflows/docs.yml
```

3. Nuevo script:

```text
docs-site/scripts/generate-deployment-metadata.mjs
```

4. Ajustes en:

```text
docs-site/package.json
```

5. Nuevo o actualizado:

```text
docs-site/README.md
```

6. Pruebas o validaciones realizadas.
7. Resumen final con:
   - archivos modificados;
   - decisiones tomadas;
   - variables que debe configurar un administrador;
   - comandos ejecutados;
   - resultado del build;
   - riesgos o pendientes.

---

# Restricciones para el agente

- No realizar commits ni push salvo solicitud explícita.
- No modificar código funcional del sistema.
- No cambiar dependencias salvo necesidad demostrable.
- No incorporar otro generador de documentación.
- No crear un repositorio central de documentación.
- No utilizar Git submodules.
- No utilizar `iframe`.
- No incluir tokens, claves o credenciales.
- No asumir que el directorio raíz puede eliminarse.
- No ocultar errores de build.
- No dejar `onBrokenLinks: 'warn'`.
- No cambiar todavía la rama fuente a `main`.

---

# Resultado final esperado

Al terminar, la documentación generada desde:

```text
v1.0.0/main
```

debe quedar publicada en:

```text
https://<dominio-eco-ort>/docs/admisiones/
```

ECO-ORT podrá agregar posteriormente un enlace a esa URL, sin almacenar ni desplegar los archivos de Docusaurus.

Cuando Admisiones pase a producción, el cambio será exclusivamente:

```text
DOCS_PUBLISH_BRANCH=main
DOCS_SOURCE_BRANCH=main
```

sin cambios adicionales en el código o en el workflow.
