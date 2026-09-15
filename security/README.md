# Admisiones security baseline

Baseline liviano de cumplimiento continuo para OWASP ASVS 5.0.0. Mantiene separados el catálogo oficial, la selección DSI, la evidencia y el código de aplicación.

## Organización

- `application.yaml`: sistema, componentes, ambientes y herramientas.
- `asvs/source/`: referencia normativa oficial; `asvs/controls/`: selección piloto; `asvs/applicability/`: interpretación y mapeos; `asvs/exceptions/`: excepciones con vencimiento.
- `evidence/evidence.jsonl`: evidencia append-only y `schema.json`: contrato.
- `policies/`: reglas de evaluación e impacto diferencial.
- `tools/`: evaluación e importadores sin dependencias.
- `reports/`: discovery y resumen generado; `adr/`: decisiones.

## Agregar un control

Copie el ID y significado desde ASVS 5.0.0, agréguelo a `asvs/controls/pilot.json` y al perfil. Declare scope, métodos, riesgo, owner y estado. No mezcle interpretación local con la fuente OWASP.

## Asociar evidencia

Agregue una línea JSON válida según `evidence/schema.json`. Debe incluir control, componente, ambiente, repositorio, commit, fuente, timestamp y resultado. El evaluador calcula un fingerprint estable, rechaza duplicados y sólo usa para el estado evidencia perteneciente al commit evaluado. Una evidencia de agente o humana positiva requiere revisión: no establece PASS.

## Ejecutar

```sh
node security/tools/evaluate.mjs
node security/tools/affected-controls.mjs <changed-file> [...]
```

El evaluador genera `reports/asvs-summary.md` y `reports/findings.json` (contrato: `reports/findings.schema.json`). Opciones útiles:

```sh
node security/tools/evaluate.mjs --commit <sha> --environment testing
node security/tools/evaluate.mjs --control-ids-file security/affected-controls.txt
```

`.github/workflows/security-baseline.yml` aplica tres modos:

- PR: detecta el diff y evalúa sólo los controles afectados; `PENDING` heredados no bloquean.
- Nightly: evalúa el catálogo completo.
- Release manual: evalúa el SHA candidato y bloquea `FAIL` alto o crítico. `PENDING`, `NEEDS_REVIEW` y `ACCEPTED_RISK` no bloquean hasta que exista una decisión organizacional más estricta.

Los artifacts contienen el resumen, `findings.json` y, en PR, los controles afectados.

## SonarQube

Exporte issues en el JSON real de SonarQube (`{"issues":[]}`), revise `asvs/applicability/sonar-mapping.json` y ejecute:

```sh
SONAR_ENVIRONMENT=testing node security/tools/import-sonar.mjs sonar-report.json
```

Un finding se importa como FAIL parcial; una ejecución sin findings no demuestra PASS global. El CI existente del backend publica cobertura `SonarQube.xml`, no un export de issues. Para automatizar este import faltan URL, token, project key y un contrato aprobado para obtener el JSON desde la API de SonarQube; no se inventó un endpoint ni se copiaron secretos.

## OWASP ZAP

El workflow `.github/workflows/security-zap.yml` usa `zaproxy/action-full-scan@v0.13.0` manualmente y cada noche cuando `ZAP_ENABLED=true`. Configure fuera del repositorio:

- secret `ZAP_TARGET_URL`;
- variable `ZAP_ENVIRONMENT`: `development`, `testing` o `preproduction`;
- opcionales: secret `ZAP_AUTH_HEADER_VALUE` y variables `ZAP_AUTH_HEADER`, `ZAP_AUTH_HEADER_SITE`.

Production se rechaza antes del scan. El workflow tiene timeout, permisos de sólo lectura y publica JSON, HTML, evidencia y reportes normalizados.

```sh
node security/tools/import-zap.mjs zap-report.json testing
```

El mapping vive en `asvs/applicability/zap-mapping.json`. La autenticación por header está soportada; login interactivo, MFA o scripts de sesión requieren una configuración ZAP externa y revisión humana. Nunca se debe guardar el target o la autenticación en el repositorio.

## Excepciones y estados

`NOT_APPLICABLE` exige justificación. Las instancias `.json` en `asvs/exceptions/` exigen control existente, motivo, riesgo, aprobador, creación, vencimiento y ticket. Una excepción activa produce `ACCEPTED_RISK`, una vencida se reporta pero no cambia el estado y ninguna excepción produce `PASS`.

```sh
node security/tools/validate-exceptions.mjs
```

Estados: PASS, FAIL, PENDING, NEEDS_REVIEW, NOT_APPLICABLE y ACCEPTED_RISK. Evidencia vencida no cuenta.

## Limitaciones

- ZAP y Sonar sólo aportan evidencia negativa o parcial; cero findings no prueba cumplimiento.
- El target, secretos y autenticación compleja de ZAP dependen del ambiente.
- La política de release pendiente de aprobación humana es si `PENDING`, `NEEDS_REVIEW` o `ACCEPTED_RISK` deben bloquear.
- Los warnings NuGet observados en el submódulo se documentan en `reports/core-dependency-findings.md`; `Core` no se modificó.

## Extensión

Amplíe catálogo y mapeos gradualmente. Prefiera test automático, regla y configuración verificable; luego scanner, runtime, revisión estructurada y revisión humana. No agregue una plataforma ni dependencia hasta que el piloto demuestre esa necesidad.
