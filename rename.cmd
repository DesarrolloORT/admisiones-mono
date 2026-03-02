@echo off
setlocal

REM Ir a la carpeta donde está este .cmd
cd /d "%~dp0"

REM Elegir PowerShell 7 si existe, si no el PowerShell clásico
where pwsh >nul 2>&1 && (set "PS=pwsh") || (set "PS=powershell")

REM Ejecutar el .ps1 (pedirá los datos en pantalla)
"%PS%" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0\rename.ps1"

set "RET=%ERRORLEVEL%"
echo.
if %RET%==0 (
  echo [OK] Renombrado completado.
  echo Sugerido: dotnet restore && dotnet build
) else (
  echo [ERROR] El script devolvio codigo %RET%.
)
echo.
pause
endlocal
