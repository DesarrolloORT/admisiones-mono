# Setup

> Tipo: how-to

Guia minima para inicializar un repositorio nuevo creado a partir de esta plantilla.

Este documento no asume que `angular-template` se mantendra como aplicacion productiva. Define la base que un proyecto nuevo debe tomar, revisar y adaptar durante su arranque.

## Entorno de referencia

- Node.js 22 como entorno base recomendado. Es la version usada por [`.devcontainer/devcontainer.json`](../.devcontainer/devcontainer.json).
- `npm` para instalar dependencias y ejecutar scripts.
- Java si se va a usar `npm run update-models` o `npm run update-api`.
- Docker Desktop y la extension Dev Containers de VS Code si se quiere trabajar dentro del contenedor.

## Instalacion

1. Instalar dependencias:

   ```bash
   npm install
   ```

2. Iniciar sesion con la cuenta ORT (una sola vez por maquina): la primera vez que se ejecute el sync de environment se abre el navegador para autenticarse con Entra ID. La sesion queda persistida (token cache cifrado con DPAPI + `tmp/env/azure-auth-record.json`); no se necesita Azure CLI. Para forzar un nuevo login: `npm run env:cache:clear`. En entornos donde no se pueda abrir el navegador, usar `npm run env:sync -- --env desa --device-code`.

   `npm run start` genera automaticamente `src/environments/generated-environment.ts` desde Azure App Configuration y actualiza `src/web.config` con la CSP del ambiente. Por defecto usa cache local durante 60 minutos y solo vuelve a Azure cuando el cache vence o se ejecuta `npm run env:refresh -- --env desa`.

   Los archivos `src/environments/generated-environment.ts` y `src/web.config` estan ignorados por [`.gitignore`](../.gitignore). El cache local vive en `tmp/env/`, tambien ignorado por Git.

3. Actualizar contratos generados si el backend Swagger ya esta disponible:

   ```bash
   npm run update-api
   ```

   Este comando ejecuta `update-models` y `update-endpoints`. Ambos leen `API_URL`
   desde el environment indicado, toman su origen y descargan
   `/swagger/v1/swagger.json` por defecto. Al terminar, compila la configuracion
   usada por `ng serve`; si el contrato nuevo rompe la aplicacion, informa los
   archivos y lineas afectados antes de que se ejecute `npm start`. Si el Swagger
   no esta disponible, conserva los modelos generados anteriores.

   Si el Swagger vive en otra ruta:

   ```bash
   npm run update-models -- --swagger-path /swagger/v2/swagger.json
   npm run update-endpoints -- --swagger-path /swagger/v2/swagger.json
   ```

   Los modelos se escriben en `src/app/shared/api/generated/models/` y los endpoints
   tecnicos en `src/app/shared/api/generated/endpoints/`. Esos archivos son
   locales, estan ignorados por Git y no deben editarse manualmente.

   Para actualizar los endpoints reales disponibles en tu ambiente local:

   ```bash
   npm run update-api
   ```

4. Ajustar la base del repositorio nuevo:
   - reemplazar `angular-template` por el slug real del proyecto;
   - revisar dependencias, workflows y scripts que no apliquen;
   - completar README y documentacion especifica del proyecto que nace desde esta plantilla.

## Ejecucion local

- `npm run start`: levanta la base en `http://localhost:4200/`.
- `npm run start:o`: levanta la base y abre el navegador.
- `npm run start:dc`: levanta la base con las opciones necesarias para desarrollo dentro del contenedor.

## Validacion minima

Antes del primer PR del repositorio nuevo conviene ejecutar:

```bash
npm run quality:local
```

Antes de abrir un PR, validar que los adapters mantengan encapsulados los contratos
generados y no expongan tipos ambiguos:

```bash
npm run check-api-contracts
```

Ese comando es de solo lectura: revisa los endpoints generados locales y las
firmas publicas de adapters.

Si se quiere anticipar el Quality Gate antes del PR, configurar `SONAR_HOST_URL`, `SONAR_TOKEN`, `SONAR_PROJECT_KEY` y un `sonar-scanner` local, y ejecutar:

```bash
npm run sonar:local
```

## Dev Container

La plantilla incluye [`.devcontainer/devcontainer.json`](../.devcontainer/devcontainer.json) con:

- imagen base `mcr.microsoft.com/vscode/devcontainers/typescript-node:22`;
- Angular CLI 20;
- instalacion de dependencias con `postCreateCommand: npm install`;
- arranque automatico con `postStartCommand: npm run start:dc`.

## Referencias relacionadas

- [README.md](../README.md)
- [docs/WORKFLOW.md](./WORKFLOW.md)
- [docs/EXTENSIONS.md](./EXTENSIONS.md)
- [docs/codegen/update-endpoints.md](./codegen/update-endpoints.md)
