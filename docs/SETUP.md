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

2. Configurar `envs-cli` (una sola vez por maquina):

   ```bash
   envs config --repo-url https://github.com/DesarrolloORT/front-envs.git
   envs login
   ```

   Los scripts `npm run start`, `start:local` y `start:preprod` generan automaticamente `src/environments/environment.generated.ts` mediante `envs run`.

   El archivo `src/environments/environment.generated.ts` esta ignorado por [`.gitignore`](../.gitignore).

3. Actualizar contratos generados si el backend Swagger ya esta disponible:

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

   Los modelos se escriben en `src/app/shared/api/generated/models/` y los endpoints
   tecnicos en `src/app/shared/api/generated/endpoints/`. Esos archivos son
   locales, estan ignorados por Git y no deben editarse manualmente.

   Para descubrir los endpoints reales disponibles en tu ambiente local:

   ```bash
   npm run api:endpoints
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
npm run lint:check
npm run test
npm run build
```

Cuando el PR depende de cambios en Swagger, tambien conviene validar si el
Swagger local elimina o renombra endpoints consumidos por adapters:

```bash
npm run check-endpoints
```

Ese comando es de solo lectura: compara el Swagger actual contra los endpoints
generados locales y reporta breaking changes o imports obsoletos.

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
