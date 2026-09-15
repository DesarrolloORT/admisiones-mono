# Baseline closure

Fecha: 2026-09-15.

## Validaciones ejecutadas

- Backend: `dotnet test WebApiAdmisiones/UnitTesting/UnitTesting.csproj` — 1222/1222 tests aprobados.
- Frontend lint: aprobado, con 11 warnings preexistentes de compatibilidad CSS y 0 errores.
- Frontend build Development: aprobado después de sincronizar ambiente y regenerar contratos.
- Frontend: 144 archivos / 972 tests aprobados; 24/24 tests de contratos aprobados.
- Security tooling: 3/3 tests aprobados.
- Excepciones: formato válido; 0 vencidas.

## Evidencia incorporada

- `v5.0.0-6.8.2`: configuración de validación JWT verificada por test automático.
- `v5.0.0-16.5.1`: ocultamiento de detalles de errores en Production verificado por test automático.

Los demás controles permanecen `PENDING`; el cierre del baseline no equivale a cumplimiento total ASVS.

## Hallazgos existentes

La restauración de `api-admisiones/Core` reportó vulnerabilidades conocidas, incluidas severidades alta y crítica, en `CoreWCF.NetFramingBase`, `CoreWCF.Primitives` y `System.Security.Cryptography.Xml`. El submódulo compartido no se modificó. Requiere ticket, evaluación de versiones corregidas y coordinación con sus propietarios.

## Integración continua

`.github/workflows/security-baseline.yml` ejecuta tests del tooling, valida excepciones, evalúa el catálogo y publica el resumen como artifact.

