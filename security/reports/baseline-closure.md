# Baseline closure

Fecha: 2026-09-15. Commit evaluado: `fbe7a8ee83a3542f8b021e25edd90eb0963fb491`.

## Validaciones ejecutadas

- Security tooling: sintaxis Node aprobada; 13/13 tests aprobados.
- Excepciones: 0 instancias, 0 activas, 0 vencidas.
- Evaluación: 8 controles; 2 PASS, 6 PENDING, 0 FAIL. `findings.json` generado.
- Backend build Debug: aprobado con 0 errores y 42 warnings; los warnings NuGet de Core están documentados.
- Backend tests: 1222/1222 aprobados.
- IDOR/BOLA focalizado: 1/1 aprobado; otro usuario recibe 403 y no se guarda el archivo.
- Frontend lint equivalente: Prettier, checks de comentarios, Stylelint, ESLint y contratos aprobaron.
- Frontend unit tests: 144 archivos de specs registrados sin fallos; cobertura `lcov.info` generada.
- Frontend contract/codegen tests: 24/24 aprobados.
- Frontend build Development: finalizó y produjo `dist/admisiones`.
- `git diff --check`: aprobado.

`npm --prefix admisiones run lint:check` no pudo iniciar Prettier porque `npm ci` no creó los enlaces `node_modules/.bin`. Los mismos entrypoints JavaScript se ejecutaron directamente y aprobaron. `npm rebuild` no pudo reconstruir `keytar` porque el entorno no tiene una instalación de Visual Studio reconocida por `node-gyp`; es una limitación del ambiente local, no un fallo de código.

`git submodule update --init --recursive` fue rechazado por la política del entorno porque podía cambiar el submódulo protegido. `git submodule status` confirmó que `api-admisiones/Core` ya estaba inicializado en `01239cdf6054dc5a450dcdaa3a867ae3204b61e7`.

## Evidencia incorporada

- `v5.0.0-6.8.2`: configuración de validación JWT verificada por la suite backend del commit evaluado.
- `v5.0.0-16.5.1`: ocultamiento de detalles de errores en Production verificado por la suite backend del commit evaluado.
- `v5.0.0-8.2.2`: evidencia informativa y parcial de una prueba real de autorización por objeto. Permanece PENDING porque no cubre todos los recursos.

Los demás controles permanecen PENDING. La evidencia de commits anteriores se muestra, pero no cambia el estado del commit evaluado.

## Hallazgos existentes

`CoreWCF.Primitives` 1.9.0, `CoreWCF.NetFramingBase` 1.9.0 y `System.Security.Cryptography.Xml` 10.0.7 tienen advisories vigentes. Presencia no equivale a explotabilidad; origen, relación directa/transitiva, severidad, versiones disponibles y condiciones se detallan en `core-dependency-findings.md`. El submódulo compartido no se modificó.

## Integración continua y gaps

- `security-baseline.yml` implementa PR afectado, nightly completo y release manual.
- `security-zap.yml` implementa ZAP manual/nightly sólo para Development, Testing o Preproduction.
- ZAP requiere target y, si aplica, autenticación externos. Login interactivo o MFA requieren intervención/configuración adicional.
- SonarQube sólo está integrado cuando exista un export real de issues; el workflow actual del backend publica cobertura, no findings. Faltan URL, token y contrato API aprobados.
- En release, FAIL alto/crítico bloquea. La organización debe decidir si PENDING, NEEDS_REVIEW o ACCEPTED_RISK también bloquean.
- No se implementó Power Automate, Teams, ClickUp ni otra notificación.
