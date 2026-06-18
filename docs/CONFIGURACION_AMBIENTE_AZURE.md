# Configuración de ambiente frontend con Azure App Configuration

Este proyecto obtiene su configuración de ambiente desde **Azure App Configuration**.  
Azure queda como fuente única de verdad y el repositorio no guarda valores reales de ambiente.

El flujo es:

```text
npm start
  → npm run env:desa
  → lee Azure App Configuration
  → genera src/environments/generated-environment.ts
  → ejecuta ng serve
```

El archivo `generated-environment.ts` es generado automáticamente y no debe editarse manualmente.

---

## 1. Requisitos previos

El desarrollador necesita:

- Acceso al repositorio.
- Node.js y npm instalados.
- Azure CLI instalada.
- Permiso de lectura sobre el recurso Azure App Configuration.

El recurso usado por este proyecto es:

```text
App Configuration: AppConfigurationDesarrolloIA
Endpoint: https://appconfigurationdesarrolloia.azconfig.io
Key: frontend:admisiones:environment
Label desa: desa
```

---

## 2. Instalar Azure CLI

En Windows, abrir **PowerShell como administrador** y ejecutar:

```powershell
winget install --exact --id Microsoft.AzureCLI
```

Luego cerrar y abrir nuevamente la terminal, VS Code o Windows Terminal.

Validar:

```powershell
az --version
```

Si el comando no se reconoce, cerrar y volver a abrir la terminal. Si sigue sin funcionar, reiniciar la PC.

Ruta estándar esperada en Windows:

```text
C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd
```

---

## 3. Iniciar sesión en Azure

Ejecutar:

```powershell
az login
```

Se abrirá el navegador para iniciar sesión con el usuario institucional.

Si Azure CLI pide seleccionar una suscripción, ingresar el número correspondiente. Por ejemplo:

```text
[1] ORT - MAI
```

Escribir:

```text
1
```

y presionar Enter.

Validar que el login quedó correcto:

```powershell
az account show
```

Si aparece una suscripción incorrecta, seleccionar la correcta:

```powershell
az account set --subscription "ORT - MAI"
```

---

## 4. Pedir permisos si corresponde

Para consumir environments desde Azure App Configuration, el usuario debe tener rol de lectura sobre el recurso:

```text
App Configuration Data Reader
```

También sirve:

```text
App Configuration Data Owner
```

pero ese rol solo debería usarse para usuarios que crean o modifican configuraciones.

Si el usuario no tiene permisos, pedir que se le asigne acceso sobre:

```text
AppConfigurationDesarrolloIA
```

Rol solicitado:

```text
App Configuration Data Reader
```

Ruta en Azure Portal:

```text
AppConfigurationDesarrolloIA
  → Control de acceso (IAM)
  → Agregar asignación de roles
  → App Configuration Data Reader
  → Seleccionar usuario o grupo
```

Para verificar el acceso:

```text
AppConfigurationDesarrolloIA
  → Control de acceso (IAM)
  → Comprobar acceso
  → Buscar usuario
```

Puede demorar algunos minutos en propagarse después de asignar el rol.

---

## 5. Instalar dependencias del proyecto

Desde el repo:

```powershell
cd C:\GIT\admisiones
npm install
```

El proyecto utiliza estas dependencias para leer Azure App Configuration:

```bash
npm install -D @azure/app-configuration @azure/identity
```

Normalmente ya deberían estar en `package.json`; no hace falta volver a instalarlas si `npm install` terminó correctamente.

---

## 6. Archivos relevantes del repo

### Script de sincronización

```text
tools/env/sync-azure-environment.mjs
```

Este script:

1. Busca Azure CLI en el PATH.
2. En Windows, si no está en el PATH, intenta agregar automáticamente la ruta estándar:
   ```text
   C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin
   ```
3. Usa la sesión de `az login`.
4. Lee la key de Azure App Configuration.
5. Compara el `ETag` de Azure contra la cache local.
6. Si cambió, regenera:
   ```text
   src/environments/generated-environment.ts
   ```
7. Si no cambió, no regenera nada.

### Environment de Angular

```ts
import { generatedEnvironment } from './generated-environment';

export const environment = generatedEnvironment;
```

Ubicación esperada:

```text
src/environments/environment.ts
```

### Archivos ignorados por Git

El archivo generado y la cache local no deben subirse al repositorio.

`.gitignore`:

```gitignore
src/environments/generated-environment.ts
.ort/env-cache/
```

---

## 7. Ejecutar el proyecto

Ejecutar:

```powershell
npm start
```

Esto ejecuta internamente:

```text
npm run env:desa && ng serve
```

Script esperado en `package.json`:

```json
{
  "scripts": {
    "env:desa": "node tools/env/sync-azure-environment.mjs --project admisiones --env desa --endpoint https://appconfigurationdesarrolloia.azconfig.io",
    "env:desa:force": "node tools/env/sync-azure-environment.mjs --project admisiones --env desa --endpoint https://appconfigurationdesarrolloia.azconfig.io --force",
    "start": "npm run env:desa && ng serve"
  }
}
```

---

## 8. Forzar regeneración del environment

Si se quiere forzar la descarga desde Azure y regenerar el archivo local:

```powershell
npm run env:desa:force
```

o directamente:

```powershell
node tools/env/sync-azure-environment.mjs --project admisiones --env desa --endpoint https://appconfigurationdesarrolloia.azconfig.io --force
```

---

## 9. Solución de problemas

### Error: `az` no se reconoce

Mensaje típico:

```text
az no se reconoce como un comando interno o externo
```

o:

```text
az: The term 'az' is not recognized
```

Solución:

1. Confirmar que Azure CLI esté instalada:
   ```powershell
   az --version
   ```
2. Cerrar y reabrir PowerShell, CMD, VS Code o Windows Terminal.
3. Validar que exista:
   ```powershell
   Test-Path "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
   ```
4. Si no existe, reinstalar Azure CLI:
   ```powershell
   winget install --exact --id Microsoft.AzureCLI
   ```

El script intenta agregar automáticamente esa ruta al PATH del proceso Node, pero Azure CLI debe estar instalada.

---

### Error: `ChainedTokenCredential authentication failed`

Causa probable: no hay sesión válida de Azure CLI o Node no puede ejecutar `az`.

Validar:

```powershell
az account show
```

Luego validar que Node pueda ejecutar Azure CLI:

```powershell
node -e "require('node:child_process').execFileSync('az', ['account', 'show'], { stdio: 'inherit', shell: true })"
```

Si falla, cerrar y reabrir la terminal o reiniciar VS Code.

---

### Error: `403 Forbidden`

Causa: el usuario autenticado no tiene permisos suficientes sobre App Configuration.

Acción:

```text
Pedir permisos: App Configuration Data Reader sobre AppConfigurationDesarrolloIA.
```

Verificar en:

```text
AppConfigurationDesarrolloIA
  → Control de acceso (IAM)
  → Comprobar acceso
```

---

### Error: no encuentra la key o el label

Debe existir exactamente:

```text
Key: frontend:admisiones:environment
Label: desa
```

Revisar en Azure Portal:

```text
AppConfigurationDesarrolloIA
  → Explorador de configuración
```

---

### Error: el value no es JSON válido

El value en Azure debe ser un objeto JSON válido.

Correcto:

```json
{
  "environment": "desa",
  "project": "admisiones",
  "apiUrl": "https://apiadmisionesdesa.ort.edu.uy"
}
```

Incorrecto:

```text
environment=desa
```

También es incorrecto dejar el value vacío si el content type es `application/json`.

---

## 10. Cómo funciona la detección de cambios

No usamos una `sentinel`.

Como todo el environment está en una única key JSON, el script usa el `ETag` de Azure App Configuration.

Flujo:

```text
npm start
  → lee frontend:admisiones:environment / label=desa
  → obtiene ETag remoto
  → compara contra .ort/env-cache/admisiones-desa.json
  → si el ETag es igual, no regenera
  → si el ETag cambió, regenera generated-environment.ts
```

Esto evita mantener una segunda key de versión manual.

---

## 11. Regla operativa

Los desarrolladores no editan:

```text
src/environments/generated-environment.ts
```

Los cambios de configuración se realizan en:

```text
Azure App Configuration
```

y luego cada dev recibe la actualización automáticamente al ejecutar:

```powershell
npm start
```

si el `ETag` cambió.

---

## 12. Resumen para un dev nuevo

1. Instalar Azure CLI:
   ```powershell
   winget install --exact --id Microsoft.AzureCLI
   ```
2. Cerrar y abrir la terminal.
3. Ejecutar:
   ```powershell
   az login
   ```
4. Validar:
   ```powershell
   az account show
   ```
5. Pedir permisos si no tiene:
   ```text
   App Configuration Data Reader sobre AppConfigurationDesarrolloIA
   ```
6. Ir al repo:
   ```powershell
   cd C:\GIT\admisiones
   ```
7. Instalar dependencias:
   ```powershell
   npm install
   ```
8. Arrancar:
   ```powershell
   npm start
   ```
