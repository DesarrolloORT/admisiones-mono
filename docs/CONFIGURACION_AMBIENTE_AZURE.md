# Configuración de ambiente frontend con Azure App Configuration

Este proyecto obtiene la configuración frontend desde Azure App Configuration.
Azure es la fuente de verdad; el repo no guarda valores reales de ambiente.

Flujo local:

```text
npm start
  → npm run generate-env:desa
  → lee Azure App Configuration
  → genera src/environments/generated-environment.ts
  → actualiza src/web.config con la CSP del ambiente
  → ejecuta ng serve
```

El archivo `generated-environment.ts` es generado automáticamente y no debe editarse manualmente.

## Requisitos

- Node.js y npm instalados.
- Azure CLI instalada.
- Sesión válida con `az login`.
- Permiso de lectura sobre Azure App Configuration.

Recurso usado por este proyecto:

```text
App Configuration: AppConfigurationDesarrolloIA
Endpoint: https://appconfigurationdesarrolloia.azconfig.io
Key: frontend:admisiones:environment
Label desa: desa
```

Rol mínimo:

```text
App Configuration Data Reader
```

## Script

```text
tools/env/sync-azure-environment.mjs
```

Que hace el script?

1. Valida que Azure CLI esté disponible.
2. Usa la sesión de `az login`.
3. Lee la key JSON desde Azure App Configuration.
4. Valida que el value sea JSON.
5. Genera `src/environments/generated-environment.ts`.
6. Genera `src/web.config` usando `CSP_POLICY` o `cspPolicy` del JSON.

## Environment de Angular

```ts
export { generatedEnvironment as environment } from './generated-environment';
```

Ubicación:

```text
src/environments/environment.ts
```

Ignorado por Git:

```gitignore
src/environments/generated-environment.ts
```

## Ejecutar

```powershell
npm start
```

Script en `package.json`:

```json
{
  "scripts": {
    "generate-env:desa": "node tools/env/sync-azure-environment.mjs --project admisiones --env desa --endpoint https://appconfigurationdesarrolloia.azconfig.io",
    "start": "npm run generate-env:desa && ng serve -o"
  }
}
```

Para regenerar sin levantar Angular:

```powershell
npm run generate-env:desa
```

## CSP

La CSP del ambiente debe estar en Azure dentro del JSON, como `CSP_POLICY` o `cspPolicy`.
El script falla si no existe, porque `web.config` se genera desde ese valor.

Ejemplo mínimo:

```json
{
  "production": false,
  "API_URL": "https://apiadmisionesdesa.ort.edu.uy",
  "RECAPTCHA_KEY": "site-key-publica",
  "CSP_POLICY": "object-src 'none'; base-uri 'self'; frame-ancestors 'self';"
}
```

`RECAPTCHA_KEY` es la site key pública usada por el navegador. El secret de reCAPTCHA nunca debe estar en frontend.

## Problemas comunes

### `az` no se reconoce

Validar:

```powershell
az --version
```

Si falla, cerrar y abrir PowerShell, CMD, VS Code o Windows Terminal. En Windows la ruta esperada suele ser:

```text
C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd
```

### No hay sesión válida

```powershell
az login
az account show
```

### `403 Forbidden`

El usuario no tiene permisos suficientes. Pedir `App Configuration Data Reader` sobre `AppConfigurationDesarrolloIA`.

### No existe la key o el label

Debe existir exactamente:

```text
Key: frontend:admisiones:environment
Label: desa
```

### El value no es JSON válido

El value en Azure debe ser un objeto JSON completo, no formato `.env`.
