<!-- Badges -->

[![Deploy to Production](https://github.com/DesarrolloORT/angular-template/actions/workflows/cd.yml/badge.svg)](https://github.com/DesarrolloORT/angular-template/actions/workflows/cd.yml)
[![Rollback to Latest Stable Release](https://github.com/DesarrolloORT/angular-template/actions/workflows/rollback.yml/badge.svg)](https://github.com/DesarrolloORT/angular-template/actions/workflows/rollback.yml)
[![Build and Attach to Release](https://github.com/DesarrolloORT/angular-template/actions/workflows/release.yml/badge.svg)](https://github.com/DesarrolloORT/angular-template/actions/workflows/release.yml)
[![Deploy to Preproduction Environment](https://github.com/DesarrolloORT/angular-template/actions/workflows/preprod-test-deploy.yml/badge.svg)](https://github.com/DesarrolloORT/angular-template/actions/workflows/preprod-test-deploy.yml)
[![Deploy to Development Environment](https://github.com/DesarrolloORT/angular-template/actions/workflows/dev-test-deploy.yml/badge.svg)](https://github.com/DesarrolloORT/angular-template/actions/workflows/dev-test-deploy.yml)
[![CI - version branches](https://github.com/DesarrolloORT/angular-template/actions/workflows/ci.yml/badge.svg)](https://github.com/DesarrolloORT/angular-template/actions/workflows/ci.yml)

# Nombre del Proyecto

_Descripción en una oración que resuma el proyecto_

## Índice

- [Nombre del Proyecto](#nombre-del-proyecto)
  - [Índice](#índice)
  - [Configuración de desarrollo local](#configuración-de-desarrollo-local)
  - [Ejecutar la aplicación en un servidor local](#ejecutar-la-aplicación-en-un-servidor-local)
  - [Entorno de desarrollo](#entorno-de-desarrollo)
  - [Prerequisitos (en caso de no usar el Dev Container)](#prerequisitos-en-caso-de-no-usar-el-dev-container)
  - [Arquitectura del Proyecto](#arquitectura-del-proyecto)
    - [Estructura de carpetas y archivos relevantes](#estructura-de-carpetas-y-archivos-relevantes)
    - [Generación de elementos de Angular](#generación-de-elementos-de-angular)
  - [Guía de Contribución](#guía-de-contribución)
    - [Cómo Contribuir:](#cómo-contribuir)
    - [Estándares y Buenas Prácticas:](#estándares-y-buenas-prácticas)
  - [Scripts](#scripts)
  - [Pre-commit hook](#pre-commit-hook)
  - [Generación de archivos de testing](#generación-de-archivos-de-testing)
    - [Estructura de los Scripts](#estructura-de-los-scripts)
    - [Configuración](#configuración)
  - [Workflows](#workflows)

---

## Configuración de desarrollo local

1. Clonar el repositorio:
   ```bash
   git clone <URL-del-repositorio>
   ```
2. Iniciar sesión en npm:
   ```bash
   npm login --registry=https://npm.pkg.github.com
   # El comando anterior le solicitará usario y contraseña.
   # En usuario ingrese su usuario de GitHub con acceso a este repositorio.
   # En contraseña ingrese un Personal Access Token (PAT) con permisos de `repo`, `read:packages` y `write:packages`.
   ```
3. Instalar las dependencias:
   ```bash
   npm install
   ```
4. Configurar los archivos de ambiente:
   Renombrar `environment.template.ts` a `environment.ts` y completar con las propiedades de entorno.

> [!NOTE]
> Crear y/o editar `environment.prod.ts`, `environment.staging.ts` y `environment.dev.ts` según corresponda.

## Ejecutar la aplicación en un servidor local

Ejecutar `npm run start` o `ng serve` para iniciar la aplicación en modo desarrollo (por defecto en el puerto 4200). `npm run start:o` inicializará la aplicación en el puerto 4200 y abrirá el navegador. Ver [Entorno de desarrollo](#entorno-de-desarrollo) para información sobre la ejecución en un servidor local dentro de un contenedor.

## Entorno de desarrollo

El proyecto está configurado con `devcontainers` para tener una única configuración del entorno de desarrollo en VS Code y evitar el _"en mi máquina funciona"_. Para poder ejecutar el entorno de desarrollo dentro de un `Dev Container`, asegúrese de tener los requisitos previos indicados [aquí](https://code.visualstudio.com/docs/devcontainers/containers#_getting-started).

El contenedor está configurado con todo lo necesario para el desarrollo de un proyecto con Angular. Si no utiliza el contenedor, asegúrese de instalar las extensiones recomendadas de VS Code. Si VS Code no le indica las extensiones recomendadas, puede verlas en el archivo [`.vscode/extensions.json`](./.vscode/extensions.json).

A continuación, una lista de las extensiones (casi) obligatorias para el correcto desarrollo de un proyecto con Angular:

Ver [extensiones recomendadas](./docs/EXTENSIONS.md).

> [!IMPORTANT]
> Al ejecutar el servidor local en un contenedor, los puertos deben ser expuestos y accedidos de una forma especial. El comando `npm run start:dc` está configurado para esto mismo. Asegurarse de acceder desde `http://localhost:4200/`.

## Prerequisitos (en caso de no usar el Dev Container)

1. Instalar [Git](https://git-scm.com/downloads/win)
2. Instalar [`nvm` para Windows](https://github.com/coreybutler/nvm-windows/releases/latest) y ejecutar `nvm install lts`.
3. Instalar `@angular/cli` globalmente: `npm install -g @angular/cli@19`.

## Arquitectura del Proyecto

_Explicación de decisiones de organización de carpetas y archivos, destacando cualquier particularidad del proyecto._

### Estructura de carpetas y archivos relevantes

_Actualizar según la estructura de carpetas y archivos que se utiliza en el proyecto._

```
└── 📁api
└── 📁coverage
└── 📁dist
└── 📁docs
    └── 📄BEST-PRACTICES.md
    └── 📄EXTENSIONS.md
    └── 📄MEDIA-QUERY-TEMPLATE.md
    └── 📄WORKFLOW.md
└── 📁public
    └── 📄favicon.ico
    └── 📄robots.txt
    └── 📄sitemap.xml
└── 📁src
    └── 📁app
        └── 📄app.config.ts
        └── 📄app.routes.ts
        └── 📁components
        └── 📁interceptors
            └── 📄http.interceptor.ts
        └── 📁services
        └── 📁utils
    └── 📁environments
        └── 📄environment.prod.ts -> Configuración de entorno para sitio de producción
        └── 📄environment.staging.ts -> Configuración de entorno para sitio de preproducción
        └── 📄environment.dev.ts -> Configuración de entorno para sitio de desarrollo/testing
        └── 📄environment.ts -> Configuración de entorno para desarrollo local
    └── 📁styles
    └── 📄index.html
    └── 📄styles.scss
    └── 📄web.config -> Configuración de la aplicación para IIS
└── 📁tests
└── 📁tools
    └── 📄swagger-codegen-cli-2.4.32.jar
└── 📄.gitignore
└── 📄.npmrc
└── 📄.prettierrc
└── 📄.stylelintrc.json
└── 📄angular.json
└── 📄package.json
└── 📄README.md
└── 📄eslint.config.mjs
└── 📄setup-vitest.ts
└── 📄vitest-global-mocks.ts
```

### Generación de elementos de Angular

Se recomienda el uso de [`@angular/cli`](https://angular.dev/cli/generate) para generar nuevos componentes, servicios, directivas, etc.
Ejecutar `ng generate directive|pipe|service|class|module <ruta>` o utilizar la extensión `Angular File Generator` para generar los elementos de Angular. Ver la [documentación de la extensión](https://marketplace.visualstudio.com/items/?itemName=imgildev.vscode-angular-generator) para obtener instrucciones de uso.

## Guía de Contribución

### Cómo Contribuir:

1. Genera una rama nueva para el desarrollo a partir de la rama `main`. Por ejemplo: `v1.2.3/main`. Esta rama actúa como un punto de referencia para las contribuciones de la versión `v1.2.3`. Para funcionalidades específicas de una versión, se pueden crear ramas adicionales, como `v1.2.3/feat/my-feature`.
2. Sigue los estándares de codificación y la estructura de carpetas establecida en el proyecto (ver [Arquitectura del Proyecto](#arquitectura-del-proyecto)).
3. Asegúrate de que las pruebas unitarias y de integración pasen localmente. Para ello, se puede ejecutar `npm run ci`.
4. Al finalizar una funcionalidad o corrección, se debe fusionar con la rama `main` de la versión (por ejemplo, `v1.2.3/main`) a través de un pull request.
5. Cada pull request debe tener un título que describa claramente el cambio realizado y una descripción detallada de los cambios realizados.
6. Para publicar una nueva versión, se debe crear un pull request a la rama `main` de la versión deseada (por ejemplo, `v1.2.3/main`). Este pull request debe tener el título `release/v1.2.3` y debe contener una descripción detallada de los cambios realizados en la versión.

### Estándares y Buenas Prácticas:

- Utiliza mensajes de commit claros y descriptivos. Ver [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).
- Respeta la estructura y convenciones establecidas en este README y en los archivos de documentación interna.
- Utilizar [SemVer](https://semver.org/) para la gestión de versiones.
- Documentar el código lo máximo posible.
- Tener en cuenta el [Angular Style Guide](https://angular.dev/style-guide).
- Cada componente, servicio, directiva, etc. debe tener un archivo de test asociado.
- Generar issues en el repositorio de la aplicación para problemas o sugerencias. Al crear el issue, se debe incluir las etiquetas correspondientes y una descripción detallada del problema o sugerencia.
- Asociar issues con versiones de la aplicación.
- Ver más buenas prácticas en este [documento](./docs/BEST-PRACTICES.md).

## Scripts

Estos son algunos de los scripts disponibles para el proyecto:

- **`start`** 🚀: Inicia el servidor local de desarrollo
  `npm run start`

- **`start:dc`** 🚀: Inicia el servidor local de desarrollo y expone la aplicación correctamente para el desarrollo dentro de un contenedor.
  `npm run start:dc`

- **`build`** 🏗️: Compila la aplicación para producción
  `npm run build`

- **`test`** 🧪: Ejecuta las pruebas unitarias con Vitest
  `npm run test`

- **`lint`** 🧹: Revisa e intenta corregir el estilo del código
  `npm run lint`

- **`ci`** 🔬: Ejecuta las pruebas de integración y validaciones automatizadas
  `npm run ci`

- **`generate-tests`** 🧪: Genera los archivos de tests unitarios a partir de la estructura de carpetas para componentes, servicios, directivas, etc. que no tengan archivos de test asociados. Estos archivos harán fallar los tests por defecto para forzar la creación de los tests y asegurar la cobertura de código.
  `npm run generate-tests`

- **`update-models`** 🔄: Actualiza los modelos de la API REST con Swagger Codegen
  `npm run update-models`

_Adapta o amplía esta lista según las necesidades específicas del proyecto._

## Pre-commit hook

Al realizar un commit, se ejecutará el hook de pre-commit. Este hook se encarga de ejecutar Prettier, Stylelint y ESLint para asegurar que el código esté formateado en el repositorio remoto.

Además, se ejecutará `npm run check-missing-tests` para asegurarse de que no se suban al repositorio componentes que no tengan su archivo de testing unitario asociado.

## Generación de archivos de testing

Este conjunto de scripts garantiza que cada archivo fuente en `src/app/` tenga su test correspondiente en `tests/`, usando Vitest. Se compone de tres scripts coordinados y un módulo compartido para evitar duplicación de código.

### Estructura de los Scripts

- **scripts/testing/check-missing-tests.js**
  Lista todos los archivos `\*.ts` (excluyendo tests) en `src/app/` que no tienen test en `tests/`, aplicando reglas de exclusión definidas en la configuración.

- **scripts/testing/generate-missing-tests.js**
  Primero intenta mover los archivos de test que puedan estar, erróneamente, en `src/app/` a `tests/` (preservando la estructura) y luego genera archivos de test placeholder para los archivos que carecen de tests. Cada archivo generado incluye un test que falla por defecto.

Todos los scripts utilizan el módulo **scripts/testing/utils.js** para recorrer archivos, aplicar exclusiones y determinar las rutas esperadas.

### Configuración

El archivo `test-generator-config.json` en la raíz del proyecto define qué archivos y rutas deben excluirse del proceso. Utiliza dos propiedades:

- **excludeFilePatterns**
  Patrones de archivos a excluir (permite wildcards y nombres específicos).
- **excludePaths**
  Rutas que, si se encuentran en la ruta relativa del archivo, hacen que éste sea excluido.
  **Ejemplos:**
  - `"legacy/"`: Excluye cualquier archivo dentro de rutas que incluyan `legacy`.
  - `"shared/testing/"`: Excluye archivos en rutas relacionadas con pruebas compartidas.

**Ejemplo:**

```json
{
  "excludeFilePatterns": ["app.config.ts", "*.routes.ts", "animations.ts"],
  "excludePaths": ["interceptors/", "shared/testing/"]
}
```

## Workflows

- **Integración Continua (CI):**
  - Los workflows se encuentran en la carpeta `.github/workflows`.
  - Automatizan la ejecución de pruebas (unitarias, de integración), análisis de calidad de código (linting, cobertura, SonarQube) y otras validaciones.
- **Despliegue Continuo (CD):**
  - Definen los pasos y variables necesarios para el despliegue en ambientes de desarrollo, preproducción y producción.
  - Incluyen scripts para rollback y notificaciones automáticas.
- **Otras Automatizaciones:**
  - Etiquetado automático de versiones y generación de builds para los releases.

Ver documentación detallada en [workflow](./docs/WORKFLOW.md).
