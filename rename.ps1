Param(
  [string]$NombreProyecto,                       # p.ej. MiProyecto
  [string]$NombreWebApi,                         # p.ej. WebApiMiProyecto (si vacío: WebApi{NombreProyecto})
  [string]$NamespaceNuevo,                       # p.ej. Mi.Empresa.MiProyecto (si vacío: NombreProyecto)
  [string]$CarpetaSolucionOriginal = "NewApi",
  [string]$SlnOriginal = "WebApiTemplate.sln",
  [string]$CarpetaWebApiOriginal = "WebApiTemplate",
  [string]$CsprojWebApiOriginal = "WebApiTemplate.csproj",
  [string]$NamespaceViejo = "WebApiFDP"
)

# --- Evitar mojibake en consola ---
try {
  [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
  $OutputEncoding = [System.Text.UTF8Encoding]::new()
} catch {}

# ---- Solicitar datos si faltan ----
if (-not $NombreProyecto) {
  $NombreProyecto = Read-Host "1) Nombre del PROYECTO (carpeta raíz + .sln)  ej: MiProyecto"
  if ([string]::IsNullOrWhiteSpace($NombreProyecto)) { Write-Error "El nombre no puede ser vacío."; exit 1 }
}
if (-not $NombreWebApi) {
  $NombreWebApi = Read-Host "2) Nombre del proyecto WEB API (carpeta interna + .csproj)  [Enter = WebApi$NombreProyecto]"
  if ([string]::IsNullOrWhiteSpace($NombreWebApi)) { $NombreWebApi = "WebApi$NombreProyecto" }
}
if (-not $NamespaceNuevo) {
  $NamespaceNuevo = Read-Host "3) Namespace NUEVO para reemplazar 'WebApiFDP'  [Enter = $NombreProyecto]"
  if ([string]::IsNullOrWhiteSpace($NamespaceNuevo)) { $NamespaceNuevo = $NombreProyecto }
}

Write-Host ""
Write-Host "Resumen:" -ForegroundColor Cyan
Write-Host "  Raíz      : '$CarpetaSolucionOriginal'  -> '$NombreProyecto'"
Write-Host "  .sln      : '$SlnOriginal'              -> '$NombreProyecto.sln'"
Write-Host "  WebApi    : '$CarpetaWebApiOriginal'    -> '$NombreWebApi'"
Write-Host "  .csproj   : '$CsprojWebApiOriginal'     -> '$NombreWebApi.csproj'"
Write-Host "  Namespace : 'WebApiFDP'                 -> '$NamespaceNuevo'"
Write-Host ""

# --- Paths base ---
$root = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
Set-Location $root

function MustExist([string]$p, [string]$what) {
  if (-not (Test-Path -LiteralPath $p)) { throw "$what no encontrado: $p" }
}

# Rutas originales
$pathCarpetaSolucion = Join-Path $root $CarpetaSolucionOriginal
$pathSlnOriginal     = Join-Path $pathCarpetaSolucion $SlnOriginal
$pathCarpetaWebApi   = Join-Path $pathCarpetaSolucion $CarpetaWebApiOriginal
$pathCsprojOriginal  = Join-Path $pathCarpetaWebApi $CsprojWebApiOriginal

MustExist $pathCarpetaSolucion "Carpeta de solución"
MustExist $pathSlnOriginal     "Archivo .sln"
MustExist $pathCarpetaWebApi   "Carpeta WebApiTemplate"
MustExist $pathCsprojOriginal  "Archivo .csproj WebApi"

# ---- 1) Renombrar carpeta raíz ----
$pathCarpetaSolucionNueva = Join-Path $root $NombreProyecto
if ($pathCarpetaSolucion -ne $pathCarpetaSolucionNueva) {
  if (Test-Path -LiteralPath $pathCarpetaSolucionNueva) { throw "Ya existe la carpeta destino: $pathCarpetaSolucionNueva" }
  Write-Host "1) Renombrando '$CarpetaSolucionOriginal' -> '$NombreProyecto'..."
  Rename-Item -LiteralPath $pathCarpetaSolucion -NewName $NombreProyecto
}
$pathCarpetaSolucion = $pathCarpetaSolucionNueva

# ---- 2) Renombrar .sln ----
$nombreSlnNuevo = "$NombreProyecto.sln"
$pathSlnNuevo   = Join-Path $pathCarpetaSolucion $nombreSlnNuevo
if ($SlnOriginal -ne $nombreSlnNuevo) {
  if (Test-Path -LiteralPath $pathSlnNuevo) { throw "Ya existe la solución destino: $pathSlnNuevo" }
  Write-Host "2) Renombrando solución '$SlnOriginal' -> '$nombreSlnNuevo'..."
  Rename-Item -LiteralPath (Join-Path $pathCarpetaSolucion $SlnOriginal) -NewName $nombreSlnNuevo
}

# ---- 3) Renombrar carpeta WebApi ----
$pathCarpetaWebApiNueva = Join-Path $pathCarpetaSolucion $NombreWebApi
if ($CarpetaWebApiOriginal -ne $NombreWebApi) {
  if (Test-Path -LiteralPath $pathCarpetaWebApiNueva) { throw "Ya existe la carpeta WebApi destino: $pathCarpetaWebApiNueva" }
  Write-Host "3) Renombrando carpeta WebApi '$CarpetaWebApiOriginal' -> '$NombreWebApi'..."
  Rename-Item -LiteralPath (Join-Path $pathCarpetaSolucion $CarpetaWebApiOriginal) -NewName $NombreWebApi
}
$pathCarpetaWebApi = $pathCarpetaWebApiNueva

# ---- 4) Renombrar .csproj WebApi ----
$csprojNuevo = "$NombreWebApi.csproj"
$pathCsprojNuevo = Join-Path $pathCarpetaWebApi $csprojNuevo
if ($CsprojWebApiOriginal -ne $csprojNuevo) {
  if (Test-Path -LiteralPath $pathCsprojNuevo) { throw "Ya existe el .csproj destino: $pathCsprojNuevo" }
  Write-Host "4) Renombrando csproj '$CsprojWebApiOriginal' -> '$csprojNuevo'..."
  Rename-Item -LiteralPath (Join-Path $pathCarpetaWebApi $CsprojWebApiOriginal) -NewName $csprojNuevo
}

# ---- 5) Reemplazar namespaces y rutas ----
Write-Host "5) Actualizando namespaces y referencias..." -ForegroundColor Yellow

# extensiones / especiales y exclusiones
$exts = @('.sln','.csproj','.cs','.props','.targets','.config','.json','.yml','.yaml',
          '.md','.ts','.tsx','.cshtml','.xml','.ps1','.sh','.dockerfile')
$specialNames = @('Dockerfile','.gitmodules')
$excludeDirs = @('\.git\', '\.vs\', '\bin\', '\obj\')

function ShouldSkip($fullPath) {
  foreach ($p in $excludeDirs) { if ($fullPath -like "*$p*") { return $true } }
  return $false
}

# Escritura segura con reintentos (locks, antivirus, etc.)
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
function WriteText-Safe([string]$path, [string]$text) {
  try {
    $fi = Get-Item -LiteralPath $path -ErrorAction Stop
    if ($fi.Attributes -band [IO.FileAttributes]::ReadOnly) { $fi.IsReadOnly = $false }
  } catch {}
  $attempts = 6
  for ($i=0; $i -lt $attempts; $i++) {
    try {
      [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
      return
    } catch [System.IO.IOException] {
      Start-Sleep -Milliseconds (200 * ($i + 1)) # backoff
    }
  }
  throw
}

$files = Get-ChildItem -LiteralPath $pathCarpetaSolucion -Recurse -File |
  Where-Object {
    -not (ShouldSkip $_.FullName) -and (
      $exts -contains $_.Extension -or $specialNames -contains $_.Name
    )
  }

foreach ($f in $files) {
  $txt = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction Stop

  # namespaces
  $txt = $txt -replace [regex]::Escape($NamespaceViejo), $NamespaceNuevo

  # nombres/rutas antiguos -> nuevos
  $txt = $txt -replace [regex]::Escape($SlnOriginal),            $nombreSlnNuevo
  $txt = $txt -replace [regex]::Escape($CarpetaWebApiOriginal),  $NombreWebApi
  $txt = $txt -replace [regex]::Escape($CsprojWebApiOriginal),   $csprojNuevo

  WriteText-Safe $f.FullName $txt
}

Write-Host ""
Write-Host "Listo ✅" -ForegroundColor Green
Write-Host "Solución: $pathSlnNuevo"
Write-Host "Sugerido: dotnet restore `"$pathSlnNuevo`" && dotnet build `"$pathSlnNuevo`""
