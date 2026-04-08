# Angular Template 2026

![Angular - v21.2.4](https://img.shields.io/badge/angular-21.2.4-c3002f) ![Angular Material - v21.2.2](https://img.shields.io/badge/@angular/material-21.2.2-fb8c00) ![Vitest](https://img.shields.io/badge/testing-vitest-6E9F18) ![SCSS](https://img.shields.io/badge/styles-scss-cc6699)

Plantilla base para proyectos Angular en Desarrollo ORT. Diseñada como **plataforma integral de desarrollo**: no solo un punto de arranque técnico, sino una base operativa, arquitectónica y contextual para desarrollar de forma consistente, rápida, mantenible y compatible con flujos de trabajo AI-assisted.

Este repositorio contiene una plantilla para iniciar cualquier proyecto en Desarrollo ORT con Angular y Angular Material.

Los documentos base de este repositorio describen la configuracion inicial que hereda un repositorio creado desde la plantilla. Cada proyecto nuevo debe revisarlos y adaptarlos a su contexto antes de considerarlos definitivos.

Documentacion base:

- [docs/SETUP.md](docs/SETUP.md)
- [docs/WORKFLOW.md](docs/WORKFLOW.md)
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [CONTRIBUTING.md](CONTRIBUTING.md)
- [CHANGELOG.md](CHANGELOG.md)

Documentacion complementaria:

- [docs/EXTENSIONS.md](docs/EXTENSIONS.md)
- [docs/BEST-PRACTICES.md](docs/BEST-PRACTICES.md)
- [docs/MEDIA-QUERY-TEMPLATE.md](docs/MEDIA-QUERY-TEMPLATE.md)

## Índice

- [Angular Template](#angular-template)
  - [Índice](#índice)
  - [Qué contiene](#qué-contiene)
  - [Cómo usarlo](#cómo-usarlo)
  - [Estructura de carpetas](#estructura-de-carpetas)
  - [Primeros pasos](#primeros-pasos)
  - [Entorno de desarrollo](#entorno-de-desarrollo)
  - [Workflows](#workflows)
  - [Testing](#testing)
  - [NPM Scripts](#npm-scripts)
  - [Pre-commit hook](#pre-commit-hook)
  - [Generación de archivos de testing](#generación-de-archivos-de-testing)
    - [Estructura de los Scripts](#estructura-de-los-scripts)
    - [Configuración](#configuración)
  - [Configuración de protección de ramas](#configuración-de-protección-de-ramas)
    - [Branch ruleset: version branches - main](#branch-ruleset-version-branches---main)
    - [Branch ruleset: main branch](#branch-ruleset-main-branch)

## Qué contiene

- Bibliotecas:
  - ![ng-recaptcha-2 - v15.0.3](https://img.shields.io/badge/ng--recaptcha--2-15.0.3-blue)
  - ![moment - v2.30.1](https://img.shields.io/badge/moment-2.30.1-black)
  - ![rxjs - v7.8.1](https://img.shields.io/badge/rxjs-7.8.1-red)
  - ![@desarrolloort/ngx-utils - v7.8.1](https://img.shields.io/badge/ngx--utils-1.0.0-662210) -- Biblioteca de utilidades para Angular desarrollada en casa (ver [documentación](https://github.com/DesarrolloORT/angular-utils/blob/main/projects/ngx-utils/README.md))
  - Todas las bibliotecas necesarias de Angular y Typescript
- Swagger Codegen para generar las interfaces de los modelos de la API REST (Ver [NPM Scripts](#npm-scripts)).
- Componente `Loader`.
- Servicio de caché integrado para manejar las peticiones de la API REST.
- Estructurado de carpetas.
- Esqueleto de variables en css.
- Configuraciones de `angular.json` en general y para ambiente de producción, pre-producción y desarrollo.
- Configuración de workflows para CI/CD y SonarQube.
- Script para generación de archivos de testing unitario siguiendo la estructura de carpetas recomendada. Al ejecutar `ng generate <tipo> <ruta>`, se generarán los archivos necesarios en la ruta indicada sin los archivos de test. Al ejecutar `npm run generate-tests`, se detectarán los componentes, servicios, directivas, etc. sin tests y generará los archivos de test correspondientes. Estos tests fallarán por defecto para forzar la creación de los tests y asegurar la cobertura de código. Este script también se ejecutará con un pre-commit hook.

## Cómo usarlo

1. Ir al [repositorio de la plantilla](https://github.com/DesarrolloORT/angular-template).

2. Hacer click en el botón "Use this template" (o "Usar esta plantilla") y presionar sobre "Create a new repository (o "Crear un nuevo repositorio").

![Ubicación del botón para usar esta plantilla](https://i.ibb.co/z8jsWx0/1.png)

4. Esta acción creará un nuevo repositorio con la estructura de la plantilla. Rellene los datos correspondientes para el proyecto y cree el nuevo repositorio.

![Pantalla de creación de repositorio](https://i.ibb.co/qB3Nv9s/Captura.png)

7. Luego de crear el nuevo repositorio, conectar su editor de código con este repositorio mediante GitHub Desktop o ejecutando el comando `git clone <URL del repositorio>` en la carpeta de preferencia, por ejemplo: `C:\User\Documents\GitHub`.

8. Una vez abierta la plantilla en el editor, hay que tener en cuenta que el código está configurado de forma genérica por lo que hay que modificar algunos valores. Presione `CTRL + SHIFT + H` para abrir la búsqueda y reemplazo del editor. En el primer campo escriba `angular-template` y en el segundo escriba el nombre del proyecto en minúsculas y si tiene un espacio entre medio, ponga un guión (`-`) en vez. Ejemplos: `sgort`, `funcionarios`, `mi-proyecto-nuevo`, etc. Luego haga un commit de ese cambio con este mensaje: `build: change app name` y puede comenzar a trabajar en el proyecto.

![](https://i.ibb.co/qB1K310/tempsnip.png)

## Estructura de carpetas

La estructura detallada y las decisiones asociadas viven en [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md).

Resumen rapido:

- `src/`: aplicacion Angular.
- `src/app/`: componentes, servicios, interceptores y utilidades.
- `src/environments/`: archivos de entorno por ambiente.
- `tests/`: tests unitarios que replican la estructura de `src/app/`.
- `scripts/testing/`: scripts para detectar o generar tests faltantes.
- `.github/workflows/`: automatizacion de CI/CD y soporte operativo del repositorio.

## Primeros pasos

La puesta en marcha detallada vive en [docs/SETUP.md](./docs/SETUP.md).

Resumen minimo:

1. Ejecutar `npm install`.
2. Crear `src/environments/environment.ts`, `src/environments/environment.dev.ts`, `src/environments/environment.staging.ts` y `src/environments/environment.prod.ts` a partir de los templates.
3. Reemplazar `angular-template` por el slug real del proyecto.
4. Revisar y adaptar configuracion de despliegue, secretos, runners y documentacion antes del primer release del proyecto nuevo.

## Entorno de desarrollo

La configuracion operativa de `devcontainers`, prerequisitos y comandos de arranque esta centralizada en [docs/SETUP.md](./docs/SETUP.md). La lista de extensiones recomendadas vive en [docs/EXTENSIONS.md](./docs/EXTENSIONS.md).

## Workflows

La documentacion de workflow vive en [docs/WORKFLOW.md](./docs/WORKFLOW.md) e incluye el flujo base recomendado para repositorios creados desde esta plantilla.

## Testing

El testing unitario está configurado con Jest. Jest es una librería de testing de JavaScript que se utiliza para realizar pruebas unitarias. Además, Jest se integra perfectamente con Angular y se puede utilizar para realizar pruebas unitarias de Angular. Para ejecutar los tests unitarios, se debe ejecutar el siguiente comando en la terminal: `npm run test`.

**Todos los archivos de testing deben estar ubicados en la carpeta `tests` y se debe replicar la estructura de directorios del proyecto.**

## NPM Scripts

Para ejecutar estos comandos, en la terminal ejecute `npm run <script>`. Ejemplo: `npm run start`.

- `start`: Compila la aplicación en un servidor local, por defecto en el puerto [4200](http://localhost:4200/).
- `start:dc`: Hace lo mismo que `start` pero expone la aplicación correctamente para el desarrollo dentro de un contenedor.
- `start:o`: Hace lo mismo que `start` pero abre el servidor en el navegador predeterminado.
- `build`: Compila la aplicación con las configuraciones de producción.
- `build:staging`: Compila la aplicación con las configuraciones de preproducción.
- `build:dev`: Compila la aplicación con las configuraciones de desarrollo.
- `ci`: Ejecuta todas las pruebas de integración incluidas en el workflow de CI Checks.
- `lint`: Ejecuta una prueba de linting en todo el proyecto y corrige los errores que son posibles de resolver de forma automática.
- `lint:check`: Ejecuta una prueba de linting en todo el proyecto.
- `test`: Ejecuta una prueba de testing en todo el proyecto una única vez.
- `test:ci`: Ejecuta una prueba de testing en un contexto de integración continua.
- `test:snapshot`: Ejecuta una prueba de testing en todo el proyecto y actualiza los snapshots.
- `test:watch`: Ejecuta una prueba de testing en todo el proyecto y queda atento a cambios en archivos.
- `test:coverage`: Ejecuta una prueba de testing en todo el proyecto una única vez y genera un reporte de cobertura. El reporte de cobertura es una métrica que nos indica que tanto código está abarcando las pruebas. Tener un porcentaje de cobertura alto no significa que todo esté funcionando correctamente, sino que estamos incluyendo la gran mayoría de líneas de código.
- `test:full`: Combina `test:watch` y `test:coverage`. Ejecuta una prueba de testing y genera un reporte de cobertura y además queda atento a futuros cambios.
- `test:only-changed`: Ejecuta una prueba de testing en todo el proyecto y queda atento a cambios en archivos.
- `generate-tests`: Utiliza el script `generate-missing-tests.js` para generar los archivos de testing que faltan.
- `check-missing-tests`: Utiliza el script `check-missing-tests.js` para listar los archivos de testing que faltan.
- `update-models`: Utiliza Swagger Codegen para generar las interfaces de los modelos de la API REST. Para utilizar este comando es necesario proveer la URL de la API.

## Pre-commit hook

El hook ejecuta `node scripts/testing/check-missing-tests.js && npx lint-staged`. Las reglas asociadas y el flujo esperado viven en [CONTRIBUTING.md](./CONTRIBUTING.md) y [docs/WORKFLOW.md](./docs/WORKFLOW.md).

## Generación de archivos de testing

Este conjunto de scripts garantiza que cada archivo fuente en `src/app/` tenga su test correspondiente en `tests/`, usando Jest. Se compone de tres scripts coordinados y un módulo compartido para evitar duplicación de código.

### Estructura de los Scripts

- **scripts/testing/check-missing-tests.js**
  Lista todos los archivos `\*.ts` (excluyendo tests) en `src/app/` que no tienen test en `tests/`, aplicando reglas de exclusión definidas en la configuración.

- **scripts/testing/generate-missing-tests.js**
  Primero intenta mover los archivos de test que puedan estar, erróneamente, en `src/app/` a `tests/` (preservando la estructura) y luego genera archivos de test placeholder para los archivos que carecen de tests. Cada archivo generado incluye un test que falla por defecto.

Todos los scripts utilizan el módulo **scripts/testing/utils.js** para recorrer archivos, aplicar exclusiones y determinar las rutas esperadas.

### Configuración

El archivo `test-generator-config.json` en la raíz del proyecto define qué archivos y rutas deben excluirse del proceso. Utiliza dos propiedades:

- **Instrucciones persistentes**: `.github/copilot-instructions.md` — reglas base del repo para cualquier agente
- **Prompts reutilizables**: `.github/prompts/` — tareas frecuentes prearmadas
- **Agentes por rol**: `.github/agents/` — planner, implementer, design-system, reviewer
- **MCP**: `.vscode/mcp.json` — Angular CLI MCP configurado

> Ver [guía de uso de IA](./docs/ai/usage.md) y [flujo con agentes](./docs/ai/agent-workflow.md).

---

## Documentación adicional

| Documento                                                           | Descripción                                    |
| ------------------------------------------------------------------- | ---------------------------------------------- |
| [Estructura de carpetas](./docs/architecture/folder-structure.md)   | Organización detallada del proyecto            |
| [Patrón de feature](./docs/architecture/feature-pattern.md)         | Cómo crear una feature nueva                   |
| [Patrón de estado](./docs/architecture/state-pattern.md)            | Manejo de estado con Signals y BehaviorSubject |
| [Estrategia de testing](./docs/testing/testing-strategy.md)         | Tipos de tests y mínimos                       |
| [Seguridad frontend](./docs/security/frontend-security.md)          | CSP, sanitización, Trusted Types               |
| [Performance baseline](./docs/performance/performance-baseline.md)  | Lazy loading, defer, imágenes, budgets         |
| [PR checklist](./docs/workflow/pull-request-checklist.md)           | Checklist para pull requests                   |
| [Branching y worktrees](./docs/workflow/branching-and-worktrees.md) | Convenciones de ramas y worktrees              |
| [Tokens del DS](./docs/design-system/tokens-usage.md)               | Cómo usar tokens del design system             |
| [Mapping DS](./docs/design-system/component-mapping.md)             | Componentes diseño ↔ código                   |
| [Uso de IA](./docs/ai/usage.md)                                     | Cómo trabajar con agentes                      |
| [Angular 21 plan](./docs/upgrades/angular-21-plan.md)               | Roadmap de evolución técnica                   |
| [Best practices](./docs/best-practices.md)                          | Buenas prácticas de desarrollo                 |
| [Workflows CI/CD](./docs/workflows.md)                              | Documentación de workflows                     |
| [Extensiones VS Code](./docs/extensions.md)                         | Extensiones recomendadas                       |

---

## Roadmap técnico

El template evoluciona de forma controlada. Los próximos pasos incluyen:

- **Angular 21**: evaluación controlada mediante spike documentado (ver [plan](./docs/upgrades/angular-21-plan.md))
- **Zoneless**: evaluación pendiente, no se adopta sin validación del ecosistema
- **Vitest**: adoptado como runner principal de unit tests
- **SSR / Hybrid rendering**: no habilitado por defecto, evaluación futura
- **Figma Code Connect**: integración progresiva con el design system

> Las decisiones técnicas se registran en [decisions.md](./migration/decisions.md).

