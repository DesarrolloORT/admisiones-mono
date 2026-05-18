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

2. Crear los archivos de entorno locales en `src/environments/`:
   - Renombrar `src/environments/environment.template.ts` a `src/environments/environment.ts`.
   - Renombrar `src/environments/environment.prod.template.ts` a `src/environments/environment.prod.ts`.
   - Crear `src/environments/environment.staging.ts`.
   - Crear `src/environments/environment.dev.ts`.

3. Completar las variables necesarias en cada archivo de entorno:
   - `RECAPTCHA_KEY`
   - `CSP_POLICY`
   - `CACHING_ENABLED`
   - `API_URL`

Los archivos `src/environments/environment.ts`, `src/environments/environment.dev.ts`, `src/environments/environment.staging.ts` y `src/environments/environment.prod.ts` estan ignorados por [`.gitignore`](../.gitignore).

4. Actualizar contratos generados si el backend Swagger ya esta disponible:

   ```bash
   npm run update-api
   ```

   Este comando ejecuta `update-models` y `update-endpoints`. Ambos leen `API_URL`
   desde el environment indicado, toman su origen y descargan
   `/swagger/v1/swagger.json` por defecto. Si el Swagger vive en otra ruta:

   ```bash
   npm run update-models -- --swagger-path /swagger/v2/swagger.json
   npm run update-endpoints -- --swagger-path /swagger/v2/swagger.json
   ```

   Los modelos se escriben en `src/app/shared/api-models/` y los endpoints
   tecnicos en `src/app/shared/api/endpoints/generated/`. Ninguno de esos
   archivos generados debe editarse manualmente.

5. Ajustar la base del repositorio nuevo:
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
npm run lint:check
npm run test
npm run build
```

Cuando el PR depende de cambios en Swagger, tambien conviene validar que los
contratos versionados no quedaron desactualizados:

```bash
npm run check-api-contracts
```

Ese comando regenera modelos y endpoints, y luego ejecuta `git diff --exit-code`
sobre `src/app/shared/api-models` y `src/app/shared/api/endpoints/generated`.

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
