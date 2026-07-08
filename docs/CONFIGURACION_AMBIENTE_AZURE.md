# Levantar el frontend con Azure y cambiar de ambiente

Este proyecto obtiene la configuración frontend desde Azure App Configuration.
Azure es la fuente de verdad; el repo no guarda valores reales de ambiente.

## Requisitos

- Node.js 20 y npm instalados.
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

## Paso a paso para levantar el frontend

1. Instalar las dependencias del proyecto:

   ```powershell
   npm install
   ```

   Si GitHub Packages rechaza la instalación, ejecutar `npm login --registry=https://npm.pkg.github.com` con un token que tenga acceso de lectura al paquete y repetir `npm install`.

2. Iniciar sesión en Azure y comprobar la cuenta activa:

   ```powershell
   az login
   az account show
   ```

3. Levantar el frontend en `desa`:

   ```powershell
   npm start
   ```

4. Abrir [http://localhost:4200/](http://localhost:4200/).

`npm start` sincroniza el label `desa`, genera `src/environments/generated-environment.ts`, actualiza `src/web.config` con la CSP del ambiente y ejecuta `ng serve`.

El archivo generado no debe editarse manualmente.

## Cambiar de ambiente

Detener el servidor, sincronizar el label requerido y levantar Angular directamente:

```powershell
npm run env:sync -- --env prod
npx ng serve
```

Reemplazar `prod` por el label disponible en Azure. Para ignorar el cache y obtener la versión más reciente:

```powershell
npm run env:refresh -- --env prod
npx ng serve
```

No usar `npm start` después de seleccionar otro ambiente: su `prestart` vuelve a sincronizar `desa`.

Para volver al ambiente de desarrollo, basta con ejecutar `npm start`.

## Flujo interno

```text
npm start
  → prestart
  → npm run env:sync -- --env desa
  → usa cache local si tiene menos de 60 minutos
  → si no hay cache fresco, lee Azure App Configuration una vez
  → cachea todos los labels disponibles para la key
  → genera src/environments/generated-environment.ts
  → actualiza src/web.config con la CSP del ambiente
  → ejecuta ng serve
```

## Guardrails

Defaults del script:

```text
Cache local: tmp/env/azure-environment-cache.json
TTL: 60 minutos
Máximo local por día: 50 lecturas Azure
Máximo cache stale de fallback: 24 horas
Label filter: *
```

La lectura Azure usa `listConfigurationSettings` con la key `frontend:<project>:environment` y `labelFilter=*`, por lo que normalmente baja todos los ambientes en una sola página/request y después elige localmente el label pedido con `--env`.

Si Azure devuelve más de 100 labels, el script falla en vez de seguir paginando para no gastar requests sin querer. En ese caso usar `--label-filter desa,prod` o un label concreto.

## Scripts

```text
npm start                # sync cacheado de desa + ng serve
npm run start:o          # sync cacheado de desa + ng serve -o
npm run build:dev        # sync cacheado de desa + build development
npm run build:prod       # refresh prod + build production
npm run env:sync -- --env desa
npm run env:refresh -- --env desa
npm run env:offline -- --env desa
npm run env:cache:clear
npm run test:env-sync
```

## Forzar o evitar Azure

Para pedir explícitamente lo último:

```powershell
npm run env:refresh -- --env desa
```

Para compilar con lo que haya cacheado sin tocar Azure:

```powershell
npm run env:offline -- --env desa
```

Para cambiar el TTL en una corrida:

```powershell
npm run env:sync -- --env desa --cache-ttl-minutes 15
```

Para limpiar cache y contador local:

```powershell
npm run env:cache:clear
```

## Environment de Angular

```ts
export { generatedEnvironment as environment } from './generated-environment';
```

Ubicación:

```text
src/environments/environment.ts
```

Ignorados por Git:

```gitignore
src/environments/generated-environment.ts
src/web.config
```

El cache vive bajo `tmp/`, que también está ignorado por Git.

## CSP y `web.config`

La CSP del ambiente debe estar en Azure dentro del JSON, como `CSP_POLICY` o `cspPolicy`.
El script falla si no existe, porque `src/web.config` se genera desde ese valor y Angular lo copia al root del build por la entrada `assets` de `angular.json`.

El `web.config` generado agrega estos headers:

```text
Cache-Control: no-cache
X-Content-Type-Options: nosniff
X-Frame-Options: SAMEORIGIN
Content-Security-Policy: <CSP_POLICY del ambiente>
Referrer-Policy: no-referrer
Permissions-Policy: camera=(), geolocation=(), microphone=()
Strict-Transport-Security: max-age=31536000; includeSubDomains
```

Tambien mantiene los MIME types de `.json` y `.webmanifest`, y la regla de rewrite que manda rutas Angular no fisicas a `/index.html`.

Ejemplo minimo:

```json
{
  "production": false,
  "API_URL": "https://apiadmisionesdesa.ort.edu.uy",
  "RECAPTCHA_KEY": "site-key-publica",
  "RECAPTCHA_NONCE": "admisiones-recaptcha-2026",
  "CSP_POLICY": "default-src 'self'; script-src 'self' 'nonce-admisiones-recaptcha-2026' 'strict-dynamic' https://www.google.com https://www.gstatic.com; connect-src 'self' https://apiadmisionesdesa.ort.edu.uy https://www.google.com; frame-src https://www.google.com https://recaptcha.google.com; object-src 'none'; base-uri 'self'; frame-ancestors 'self';"
}
```

`RECAPTCHA_KEY` es la site key publica usada por el navegador. El secret de reCAPTCHA nunca debe estar en frontend. Si `RECAPTCHA_KEY` tiene valor, `CSP_POLICY` debe permitir los origenes de Google indicados en el ejemplo; si el ambiente no usa captcha, no hace falta permitirlos ni definir `RECAPTCHA_NONCE`.

`RECAPTCHA_NONCE` debe coincidir exactamente con el nonce incluido en `script-src` (`'nonce-<valor>'`). Angular lo pasa al `<script>` que carga `api.js` de Google (via `RECAPTCHA_LOADER_OPTIONS.onBeforeLoad`), y Google propaga ese mismo nonce a los scripts inline que agrega despues. Como el sitio se sirve como archivos estaticos desde IIS (sin render por request), no es posible generar un nonce distinto por response; por eso se usa un valor fijo por ambiente combinado con `'strict-dynamic'` en vez de los hashes `sha256-...` que se usaban antes. Los hashes se rompen sin aviso cuando Google cambia el contenido del script inline; el nonce fijo + `strict-dynamic` no depende de ese contenido.

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
