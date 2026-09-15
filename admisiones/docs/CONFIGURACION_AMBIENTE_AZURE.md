# Levantar el frontend con Azure y cambiar de ambiente

Este proyecto obtiene la configuración frontend desde Azure App Configuration mediante
`@desarrolloort/azure-env-sync`. Azure es la fuente de verdad; el repo no guarda valores
reales ni lógica propia de sincronización.

## Requisitos

- Node.js 20 y npm instalados.
- Cuenta ORT con permiso de lectura sobre Azure App Configuration

La convención compartida lee `frontend:<proyecto>:environment` con el label del ambiente. El
valor es el JSON final para Angular; no hay presets ni transformaciones por aplicación.

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

2. Iniciar sesión con la cuenta ORT: la primera vez que corra el sync se abre el navegador para autenticarse con Entra ID. La sesión queda persistida (token cache cifrado con DPAPI + `tmp/env/azure-auth-record.json`), por lo que los siguientes usos son silenciosos. Para cerrar la sesión local: `npm run env:sync -- desa clear-cache`. Si no se puede abrir el navegador, agregar `device-code` al comando de sync.

3. Levantar el frontend en `desa`:

   ```powershell
   npm start -- desa
   ```

4. Abrir [http://localhost:4200/](http://localhost:4200/).

`npm start` pregunta el ambiente si no recibe `--env`, genera `src/environments/generated-environment.ts`, actualiza `src/web.config` con la CSP elegida y ejecuta `ng serve`.
Si se elige `local`, también pregunta qué label (`desa`, `preprod` o `testing`) usar para
`frontend:fdp:api_base`.

El archivo generado no debe editarse manualmente.

## Cambiar de ambiente

Los ambientes válidos son `desa`, `testing`, `preprod`, `local` y `prod`.
`npm run env:sync` pregunta cuál usar; en automatizaciones se pasa
`npm run env:sync -- <ambiente>`.

## Flujo interno

```text
npm start
  → pregunta el ambiente
  → ort-azure-env admisiones --env <ambiente>
  → usa cache local si tiene menos de 60 minutos
  → si no hay cache fresco, lee Azure App Configuration una vez
  → genera src/environments/generated-environment.ts
  → actualiza src/web.config con la CSP del ambiente
  → ejecuta ng serve
```

## Guardrails

Defaults del paquete compartido:

```text
Cache local: tmp/env/azure-environment-cache.json
TTL: 60 minutos
Máximo local por día: 50 lecturas Azure
Máximo cache stale de fallback: 24 horas
```

Autenticación, cache, límites y lectura de Azure pertenecen a
`@desarrolloort/azure-env-sync`; este repo solo indica proyecto y ambiente.

## Scripts

```text
npm start
npm run build
npm run env:sync
npm start -- testing
npm run build -- prod
npm run env:sync -- local
```

Los comandos interactivos con `local` solicitan el label de FDP antes de sincronizar.

## Forzar o evitar Azure

Para pedir explícitamente lo último:

```powershell
npm run env:sync -- desa refresh
```

Para compilar con lo que haya cacheado sin tocar Azure:

```powershell
npm run env:sync -- desa offline
```

Para cambiar el TTL en una corrida:

```powershell
npm run env:sync -- desa cache-ttl-minutes=15
```

Para limpiar cache y contador local:

```powershell
npm run env:sync -- desa clear-cache
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

La CSP del ambiente debe estar en Azure dentro del JSON, como `CSP_POLICY_TEMPLATE`.
El script falla si no existe. `CSP_POLICY_TEMPLATE` acepta los placeholders `{{API_URL}}` y
`{{FDP_API_URL}}`, que `@desarrolloort/azure-env-sync` reemplaza por los valores reales del
ambiente antes de generar `CSP_POLICY`; si queda algun placeholder sin resolver, tambien falla.
`src/web.config` se genera desde ese `CSP_POLICY` ya resuelto y Angular lo copia al root del
build por la entrada `assets` de `angular.json`.

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

Ejemplo minimo. Notar que `API_URL` y `FDP_API_URL` no se declaran aca: el propio paquete los
resuelve desde `frontend:<proyecto>:api_base` y `frontend:fdp:api_base` y los inyecta en el
template.

```json
{
  "RECAPTCHA_KEY": "site-key-publica",
  "RECAPTCHA_NONCE": "admisiones-recaptcha-2026",
  "CSP_POLICY_TEMPLATE": "default-src 'self'; script-src 'self' 'nonce-admisiones-recaptcha-2026' 'strict-dynamic' https://www.google.com https://www.gstatic.com; connect-src 'self' {{API_URL}} https://www.google.com; frame-src https://www.google.com https://recaptcha.google.com {{FDP_API_URL}}; object-src 'none'; base-uri 'self'; frame-ancestors 'self';"
}
```

`RECAPTCHA_KEY` es la site key publica usada por el navegador. El secret de reCAPTCHA nunca debe estar en frontend. Si `RECAPTCHA_KEY` tiene valor, `CSP_POLICY_TEMPLATE` debe permitir los origenes de Google indicados en el ejemplo; si el ambiente no usa captcha, no hace falta permitirlos ni definir `RECAPTCHA_NONCE`.

`RECAPTCHA_NONCE` debe coincidir exactamente con el nonce incluido en `script-src` (`'nonce-<valor>'`). Angular lo pasa al `<script>` que carga `api.js` de Google (via `RECAPTCHA_LOADER_OPTIONS.onBeforeLoad`), y Google propaga ese mismo nonce a los scripts inline que agrega despues. Como el sitio se sirve como archivos estaticos desde IIS (sin render por request), no es posible generar un nonce distinto por response; por eso se usa un valor fijo por ambiente combinado con `'strict-dynamic'` en vez de los hashes `sha256-...` que se usaban antes. Los hashes se rompen sin aviso cuando Google cambia el contenido del script inline; el nonce fijo + `strict-dynamic` no depende de ese contenido.

`img-src` debe incluir `blob:` ademas de `'self' data:`. El preview/compresion de imagenes (`ImageCompressionUtils` de `@desarrolloort/ngx-utils`) genera URLs `blob:` con `URL.createObjectURL`; sin `blob:` en `img-src`, el navegador bloquea esas imagenes en la subida de identidad y OCR de registro.

## Problemas comunes

### No se abre el navegador para el login

Ejecutar el sync con device code y seguir las instrucciones en consola:

```powershell
npm run env:sync -- desa device-code
```

### No hay sesión válida o el login falla

Borrar la sesión local y reintentar (vuelve a pedir login por navegador):

```powershell
npm run env:sync -- desa clear-cache
npm run env:sync -- desa refresh
```

Si aparece un error `AADSTS...` de Entra ID, reportarlo a operaciones: puede ser una política del tenant bloqueando el flujo interactivo.

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
