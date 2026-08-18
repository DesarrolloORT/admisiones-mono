# Setup

> Tipo: how-to

Guia minima para inicializar un repositorio nuevo creado a partir de esta plantilla.

Este documento no asume que `angular-template` se mantendra como aplicacion productiva. Define la base que un proyecto nuevo debe tomar, revisar y adaptar durante su arranque.

## Entorno de referencia

- Node.js 22 como entorno base recomendado. Es la version usada por [`.devcontainer/devcontainer.json`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.devcontainer/devcontainer.json).
- `npm` para instalar dependencias y ejecutar scripts.
- Java si se va a usar `npm run update-api`.
- Docker Desktop y la extension Dev Containers de VS Code si se quiere trabajar dentro del contenedor.

## Instalacion

1. Instalar dependencias:

   ```bash
   npm install
   ```

2. Iniciar sesion con la cuenta ORT (una sola vez por maquina): la primera vez que se ejecute el sync de environment se abre el navegador para autenticarse con Entra ID. La sesion queda persistida (token cache cifrado con DPAPI + `tmp/env/azure-auth-record.json`); no se necesita Azure CLI. Para forzar un nuevo login: `npm run env:sync -- desa clear-cache`. En entornos donde no se pueda abrir el navegador, usar `npm run env:sync -- desa device-code`.

   `npm start` pregunta el ambiente, genera automaticamente `src/environments/generated-environment.ts` desde Azure App Configuration y actualiza `src/web.config` con su CSP. Por defecto usa cache local durante 60 minutos; para actualizarlo se usa `npm run env:sync -- desa refresh`.

   Los archivos `src/environments/generated-environment.ts` y `src/web.config` estan ignorados por [`.gitignore`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.gitignore). El cache local vive en `tmp/env/`, tambien ignorado por Git.

3. Generar los contratos desde el snapshot de API versionado:

   ```bash
   npm run update-api
   ```

   Este comando ejecuta `update-models`, `update-endpoints` y `update-contracts`
   leyendo `.api-spec/` (snapshot de `swagger.json` + contratos de formulario
   versionado en el repo), sin acceso de red al backend. Al terminar, compila la
   configuracion usada por `ng serve`; si el contrato nuevo rompe la aplicacion,
   informa los archivos y lineas afectados antes de que se ejecute `npm start`.

   Los modelos se escriben en `src/app/shared/api/generated/models/` y los endpoints
   tecnicos en `src/app/shared/api/generated/endpoints/`. Esos archivos son
   locales, estan ignorados por Git y no deben editarse manualmente.

4. Refrescar el snapshot cuando el backend cambia el contrato (requiere red interna ORT):

   ```bash
   npm run api-spec:refresh
   npm run update-api
   ```

   `api-spec:refresh` descarga Swagger y `/contracts` desde el origen que resuelve
   `API_URL` en `src/environments/generated-environment.ts`, o sea el ambiente que
   tengas sincronizado con `npm run env`. **Verifica cual tenes sincronizado antes
   de refrescar**: el snapshot que commitees pasa a ser el contrato de todo el
   equipo y de CI.

   Una vez commiteado, CI genera desde ese mismo snapshot, asi que local y CI
   compilan contra los mismos tipos y el ambiente que sincronice el pipeline no
   influye en los contratos.

   El diff de `.api-spec/` se revisa en el PR junto con el codigo que lo consume;
   asi un cambio de backend nunca rompe CI por sorpresa. Para leer otro origen
   puntualmente: `npm run api-spec:refresh -- --origin https://otra-api.ort.edu.uy`.

5. Ajustar la base del repositorio nuevo:
   - reemplazar `angular-template` por el slug real del proyecto;
   - revisar dependencias, workflows y scripts que no apliquen;
   - completar README y documentacion especifica del proyecto que nace desde esta plantilla.

## Ejecucion local

- `npm start`: pregunta el ambiente y levanta la base en `http://localhost:4200/`.
- `npm start -- desa -o`: levanta la base y abre el navegador.
- `npm start -- desa --host=0.0.0.0 --disable-host-check`: variante para el Dev Container.

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

La plantilla incluye [`.devcontainer/devcontainer.json`](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/.devcontainer/devcontainer.json) con:

- imagen base `mcr.microsoft.com/vscode/devcontainers/typescript-node:22`;
- Angular CLI 20;
- instalacion de dependencias con `postCreateCommand: npm install`;
- arranque automatico con `postStartCommand: npm start -- desa --host=0.0.0.0 --disable-host-check`.

## Referencias relacionadas

- [README.md](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/README.md)
- [docs/WORKFLOW.md](./WORKFLOW.md)
