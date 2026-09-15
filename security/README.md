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

Agregue una línea JSON válida según `evidence/schema.json`. Debe incluir control, componente, repositorio, commit, fuente, timestamp y resultado. Una evidencia de agente o humana positiva requiere revisión: no establece PASS.

## Ejecutar

```sh
node security/tools/evaluate.mjs
node security/tools/affected-controls.mjs <changed-file> [...]
```

El evaluador genera `reports/asvs-summary.md`. En PR use archivos cambiados y checks rápidos; nightly ejecute evaluación y scanners amplios; release debe usar evidencia del commit candidato y convertir fallos críticos en gate.

## SonarQube

Exporte issues en JSON, revise `asvs/applicability/sonar-mapping.json` y ejecute:

```sh
node security/tools/import-sonar.mjs sonar-report.json
```

Un finding se importa como FAIL parcial; una ejecución sin findings no demuestra PASS global.

## OWASP ZAP

Configure el target fuera del repositorio (`ZAP_TARGET_URL`) y escanee únicamente Development, Testing o Preproduction:

```sh
node security/tools/import-zap.mjs zap-report.json testing
```

El importador rechaza Production y no maneja credenciales.

## Excepciones y estados

`NOT_APPLICABLE` exige justificación. `ACCEPTED_RISK` exige motivo, aprobador, fechas, vencimiento y ticket. Estados: PASS, FAIL, PENDING, NEEDS_REVIEW, NOT_APPLICABLE y ACCEPTED_RISK. Evidencia vencida no cuenta.

## Extensión

Amplíe catálogo y mapeos gradualmente. Prefiera test automático, regla y configuración verificable; luego scanner, runtime, revisión estructurada y revisión humana. No agregue una plataforma ni dependencia hasta que el piloto demuestre esa necesidad.

