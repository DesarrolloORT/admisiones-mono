<!-- Badges -->

[![Deploy to Production](https://github.com/DesarrolloORT/admisiones/actions/workflows/cd.yml/badge.svg)](https://github.com/DesarrolloORT/admisiones/actions/workflows/cd.yml)
[![Rollback to Latest Stable Release](https://github.com/DesarrolloORT/admisiones/actions/workflows/rollback.yml/badge.svg)](https://github.com/DesarrolloORT/admisiones/actions/workflows/rollback.yml)
[![Build and Attach to Release](https://github.com/DesarrolloORT/admisiones/actions/workflows/release.yml/badge.svg)](https://github.com/DesarrolloORT/admisiones/actions/workflows/release.yml)
[![Deploy to Preproduction Environment](https://github.com/DesarrolloORT/admisiones/actions/workflows/preprod-test-deploy.yml/badge.svg)](https://github.com/DesarrolloORT/admisiones/actions/workflows/preprod-test-deploy.yml)
[![Deploy to Development Environment](https://github.com/DesarrolloORT/admisiones/actions/workflows/dev-test-deploy.yml/badge.svg)](https://github.com/DesarrolloORT/admisiones/actions/workflows/dev-test-deploy.yml)
[![CI - version branches](https://github.com/DesarrolloORT/admisiones/actions/workflows/ci.yml/badge.svg)](https://github.com/DesarrolloORT/admisiones/actions/workflows/ci.yml)

# Admisiones

Migracion de `admisiones_legacy` hacia una aplicacion Angular moderna, con nueva arquitectura frontend y una experiencia visual completamente renovada.

## Indice

- [Admisiones](#admisiones)
  - [Indice](#indice)
  - [Objetivo del proyecto](#objetivo-del-proyecto)
  - [Requisitos](#requisitos)
  - [Inicio rapido](#inicio-rapido)
  - [Configuracion de desarrollo local](#configuracion-de-desarrollo-local)
  - [Ejecutar la aplicacion en un servidor local](#ejecutar-la-aplicacion-en-un-servidor-local)
  - [Entorno de desarrollo](#entorno-de-desarrollo)
  - [Prerequisitos (en caso de no usar el Dev Container)](#prerequisitos-en-caso-de-no-usar-el-dev-container)
  - [Arquitectura del proyecto](#arquitectura-del-proyecto)
  - [Guia de contribucion](#guia-de-contribucion)
  - [Accesibilidad](#accesibilidad)
  - [Scripts](#scripts)
  - [Pre-commit hook](#pre-commit-hook)
  - [Generacion de archivos de testing](#generacion-de-archivos-de-testing)
  - [Workflows](#workflows)

---

## Objetivo del proyecto

> Documentacion del proyecto (SharePoint): [Proyecto Nuevo Sitio de Admisiones](https://orteduuy.sharepoint.com/:f:/r/sites/DESARROLLO/Documentos%20compartidos/2-Proyectos%20y%20Sistemas/PROYECTOS/PROYECTO%20Nuevo%20Sitio%20de%20Admisiones?csf=1&web=1&e=KeFVQs)

Este repositorio representa la evolucion de la aplicacion legacy `admisiones_legacy` hacia una base Angular actualizada, mantenible y alineada con las practicas de Desarrollo ORT.

El alcance incluye:

- Migracion progresiva de funcionalidades del sistema legacy.
- Redisenio completo de interfaz y experiencia de usuario.
- Estandarizacion de arquitectura, testing y CI/CD sobre esta nueva base.

## Requisitos

| Herramienta | Version minima | Notas                                |
| ----------- | -------------- | ------------------------------------ |
| Node.js     | 20.x           | Recomendado usar LTS                 |
| npm         | 10.x           | Incluido con Node.js                 |
| Angular CLI | 21.x           | Solo para desarrollo local           |
| Docker      | Opcional       | Requerido para entorno en contenedor |

## Inicio rapido

1. Clonar el repositorio.
2. Instalar dependencias con `npm install`.
3. Crear ambientes a partir de los templates en `src/environments/`.
4. Ejecutar `npm run start`.

Si necesitas el flujo completo con autenticacion de packages y detalle de ambientes, seguir la seccion de configuracion de desarrollo local.

## Configuracion de desarrollo local

1. Clonar el repositorio:

   ```bash
   git clone <URL-del-repositorio>
   ```

2. Iniciar sesion en npm:

   ```bash
   npm login --registry=https://npm.pkg.github.com
   # El comando anterior le solicitara usuario y contrasenia.
   # En usuario ingrese su usuario de GitHub con acceso a este repositorio.
   # En contrasenia ingrese un Personal Access Token (PAT)
   # con permisos de `repo`, `read:packages` y `write:packages`.
   ```

3. Instalar las dependencias:

   ```bash
   npm install
   ```

4. Configurar el ambiente:
   Ejecutar `envs login` (una sola vez) y luego usar `npm run start` o `npm run start:dev` para generar automaticamente `environment.generated.ts`.

## Ejecutar la aplicacion en un servidor local

Ejecutar `npm run start` o `ng serve` para iniciar la aplicacion en modo desarrollo (por defecto en el puerto 4200). `npm run start:o` inicializara la aplicacion en el puerto 4200 y abrira el navegador. Ver [Entorno de desarrollo](#entorno-de-desarrollo) para informacion sobre la ejecucion en un servidor local dentro de un contenedor.

## Entorno de desarrollo

El proyecto esta configurado con `devcontainers` para tener una unica configuracion del entorno de desarrollo en VS Code y evitar el "en mi maquina funciona".

Documentacion relacionada:

- [docs/SETUP.md](docs/SETUP.md)
- [docs/WORKFLOW.md](docs/WORKFLOW.md)
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/BEST-PRACTICES.md](docs/BEST-PRACTICES.md)
- [docs/ACCESSIBILITY.md](docs/ACCESSIBILITY.md)
- [docs/E2E-GUARDRAILS.md](docs/E2E-GUARDRAILS.md)
- [docs/codegen/update-endpoints.md](docs/codegen/update-endpoints.md)

> [!IMPORTANT]
> Al ejecutar el servidor local en un contenedor, los puertos deben ser expuestos y accedidos de una forma especial. El comando `npm run start:dc` esta configurado para esto mismo. Asegurarse de acceder desde `http://localhost:4200/`.

## Prerequisitos (en caso de no usar el Dev Container)

1. Instalar [Git](https://git-scm.com/downloads/win)
2. Instalar [`nvm` para Windows](https://github.com/coreybutler/nvm-windows/releases/latest) y ejecutar `nvm install lts`.
3. Instalar `@angular/cli` globalmente: `npm install -g @angular/cli@21`.

## Arquitectura del proyecto

La arquitectura y convenciones del proyecto se documentan en [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

Estructura principal:

```text
docs/
public/
scripts/testing/
src/
  app/
    core/
    features/
    shared/
  environments/
tools/
```

Las features deben seguir el flujo
`pages/components -> services -> endpoint adapter -> ApiHttpClient -> generated -> API`.
Ver [docs/BEST-PRACTICES.md](docs/BEST-PRACTICES.md) para las reglas de capas.

Los contratos tecnicos de la API se generan desde Swagger. Cuando cambia el
backend, ejecutar `npm run update-api` para regenerar modelos en
`src/app/shared/api/generated/models/` y endpoints en
`src/app/shared/api/generated/endpoints/`. Esos archivos son locales y estan
ignorados por Git.

`ApiHttpClient` resuelve las URLs, consume los endpoints generados y cachea por
defecto los `GET` sin parámetros. Los services consumen adapters de feature; si
necesitas ver los endpoints reales locales, ejecuta `npm run api:endpoints`.

Se recomienda utilizar `@angular/cli` para generar nuevos componentes, servicios y directivas.

## Guia de contribucion

Ver [CONTRIBUTING.md](CONTRIBUTING.md) para reglas de flujo, calidad y convenciones de colaboracion.

## Accesibilidad

El portal debe cumplir WCAG 2.2 AA en los flujos visibles. Las pautas, comandos
de validacion, criterios manuales y gaps de ORT Components estan documentados en
[docs/ACCESSIBILITY.md](docs/ACCESSIBILITY.md).

## Scripts

Estos son algunos de los scripts disponibles para el proyecto:

- `start`: inicia el servidor local de desarrollo.
- `start:dc`: inicia el servidor local para desarrollo dentro de contenedor.
- `start:o`: inicia el servidor local y abre el navegador.
- `build`: compila la aplicacion para produccion.
- `build:staging`: compila la aplicacion para preproduccion.
- `build:dev`: compila la aplicacion para desarrollo.
- `copy-web-config`: copia configuracion de IIS para builds `dev` y `staging`.
- `lint`: revisa y corrige el estilo del codigo.
- `lint:check`: valida formato y estilo sin modificar archivos.
- `test`: ejecuta las pruebas unitarias.
- `test:ci`: ejecuta las pruebas unitarias para CI.
- `test:a11y`: ejecuta Playwright + axe en desktop y mobile.
- `test:e2e:smoke`: ejecuta los E2E rapidos y bloqueantes para PR.
- `test:e2e:regression`: ejecuta manualmente flujos completos antes de releases,
  hotfixes delicados o cambios en registro/login/datos personales.
- `test:e2e:ui`: abre Playwright UI para elegir y observar cualquier E2E.
- `test:e2e:report`: abre el reporte HTML de la ultima corrida Playwright.
- `test:coverage`: ejecuta pruebas con reporte de cobertura.
- `test:watch`: ejecuta pruebas en modo observacion.
- `test:sonar`: ejecuta pruebas con cobertura para analisis de calidad.
- `ci`: ejecuta validaciones principales de CI (`lint:check`, `test:ci`, `build`, `test:a11y`, `test:e2e:smoke`).
- `generate-tests`: genera tests faltantes para archivos fuente sin test asociado.
- `check-missing-tests`: lista archivos fuente sin test asociado.
- `update-models`: actualiza modelos de API REST con Swagger Codegen.
- `update-endpoints`: actualiza constantes tipadas de endpoints desde Swagger.
- `update-api`: actualiza modelos y endpoints, y valida que Angular siga compilando.
- `check-endpoints`: regenera contratos de endpoints y falla si quedan diferencias en Git.

## Pre-commit hook

Al realizar un commit, se ejecuta el hook de pre-commit para validar formato y estilo, y para ejecutar `npm run check-missing-tests`.

## Generacion de archivos de testing

Este conjunto de scripts garantiza que cada archivo fuente en `src/app/` tenga su test correspondiente, usando Vitest y reglas de convencion del proyecto.

- `scripts/testing/check-missing-tests.js`
  Lista archivos `*.ts` (excluyendo tests) en `src/app/` que no tienen test asociado.

- `scripts/testing/generate-missing-tests.js`
  Mueve tests ubicados erroneamente y genera placeholders para los faltantes.

Todos los scripts utilizan `scripts/testing/utils.js` para recorrido de archivos, exclusiones y resolucion de rutas esperadas.

La configuracion vive en `test-generator.config.json`:

- `excludeFilePatterns`: patrones de archivo excluidos.
- `excludePaths`: rutas parciales excluidas.

## Workflows

- Integracion continua (CI): ejecucion automatizada de pruebas, lint y validaciones de calidad.
- Despliegue continuo (CD): despliegues a desarrollo, preproduccion y produccion.
- Otras automatizaciones: soporte de releases, rollback y operaciones asociadas.

Ver documentacion detallada en [docs/WORKFLOW.md](docs/WORKFLOW.md).
