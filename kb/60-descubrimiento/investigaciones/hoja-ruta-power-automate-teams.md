---
status: investigation
owner:
updated: 2026-09-15
---

# Hoja de ruta — Power Automate y Microsoft Teams

## Objetivo

Notificar resultados del baseline ASVS en Teams mediante Power Automate, manteniendo Git, GitHub Actions y sus artifacts como fuente de verdad. Teams será un canal de aviso y revisión humana, no el repositorio de evidencia ni el motor que determina `PASS`.

## Responsabilidades

### Responsable de Power Automate

- Crear y proteger el flujo.
- Configurar conexión, equipo, canal y Adaptive Card.
- Implementar autenticación, validación, deduplicación y manejo de errores.
- Documentar propietarios, operación y recuperación.

### Responsable del repositorio

- Generar `findings.json` con contrato estable.
- Invocar el flujo desde GitHub Actions usando un secret.
- Enviar enlaces al workflow, commit y artifact, sin secretos ni datos sensibles.
- Mantener estados ASVS y evidencia en `security/`.

## Arquitectura propuesta

```text
GitHub Actions
  ├─ tests y scanners
  ├─ evidence + asvs-summary.md
  └─ findings.json
          │ HTTPS autenticado
          ▼
Power Automate
  ├─ valida y deduplica
  ├─ enruta por severidad
  └─ publica Adaptive Card
          ▼
Microsoft Teams
  ├─ resumen y enlaces
  └─ reconocimiento humano
```

## Contrato mínimo de entrada

Validar y acordar este contrato antes de implementar:

```json
{
  "schemaVersion": "1.0",
  "application": "admisiones",
  "repository": "DesarrolloORT/admisiones-mono",
  "commitSha": "<sha completo>",
  "environment": "testing",
  "run": {
    "id": "<github-run-id>",
    "url": "<github-run-url>",
    "createdAt": "2026-09-15T18:30:00Z"
  },
  "summary": { "pass": 0, "fail": 1, "pending": 0, "needsReview": 0 },
  "findings": [
    {
      "id": "admisiones:v5.0.0-13.3.1:dependency:corewcf-primitives",
      "controlId": "v5.0.0-13.3.1",
      "severity": "critical",
      "component": "backend",
      "status": "FAIL",
      "title": "Dependencia con vulnerabilidad conocida",
      "description": "Resumen sin secretos, PII ni trazas sensibles.",
      "evidenceUrl": "<artifact-or-run-url>",
      "fingerprint": "<stable-fingerprint>"
    }
  ]
}
```

`id` o `fingerprint` debe permanecer estable para evitar duplicados. Si la evidencia contiene información sensible, enviar únicamente un enlace con acceso controlado.

## Adaptive Card mínima

Mostrar aplicación, ambiente, commit, conteos, máxima severidad, findings críticos/altos y enlaces al workflow y reporte. Acciones iniciales: `Abrir ejecución`, `Abrir evidencia` y `Reconocer`.

`Reconocer` solo confirma recepción. No puede cambiar un control a `PASS`, aceptar riesgo ni modificar `main`.

## Fases

### Fase 1 — Notificación unidireccional

1. Crear flujo y canal de prueba.
2. Recibir HTTP POST autenticado.
3. Validar versión, aplicación, ambiente, commit y tamaño.
4. Publicar una tarjeta resumen.
5. Responder `2xx` solo tras confirmación de Teams.
6. Probar payload válido, inválido, repetido y fallo de Teams.

### Fase 2 — Deduplicación y severidad

1. Agrupar reejecuciones mediante `fingerprint`.
2. Notificar inmediatamente `critical`/`high` y resumir el resto.
3. Evitar una tarjeta por finding.
4. Definir una ventana de reaviso.
5. Hacer observable el error sin registrar el payload sensible.

### Fase 3 — Revisión humana

1. Capturar identidad, timestamp y comentario.
2. Devolver una decisión estructurada a un destino auditado.
3. Convertir excepciones en PR o solicitud de revisión, nunca escritura directa a `main`.
4. Exigir motivo, aprobador, referencia y vencimiento para `ACCEPTED_RISK`.

Esta fase queda fuera del piloto inicial salvo necesidad confirmada.

## Seguridad

- Guardar URL/token del flujo como GitHub Secret.
- Restringir edición del flujo y acceso al canal.
- Aplicar mínimo privilegio a las conexiones de Teams.
- Validar tamaño, tipos, enums y URLs.
- No enviar secretos, tokens, cookies, PII, documentos ni trazas completas.
- Rechazar `production` durante el piloto salvo aprobación específica.
- Documentar propietario, rotación y baja de credenciales.
- Acordar retención de runs y mensajes.

## Comportamiento en CI

- La evaluación no depende de la disponibilidad de Teams.
- Fallar si no puede generarse `findings.json`.
- Un fallo de notificación genera warning/retry acotado, sin cambiar el resultado ASVS.
- Decidir aparte si la notificación será obligatoria en releases.

```text
POWER_AUTOMATE_SECURITY_WEBHOOK   # GitHub Secret
SECURITY_TEAMS_NOTIFICATIONS      # variable true|false
SECURITY_NOTIFICATION_ENVIRONMENT # testing|preproduction
```

## Pruebas de aceptación

- Payload válido publica exactamente una tarjeta.
- El mismo `fingerprint` no produce spam.
- Payload inválido no se publica.
- Findings críticos quedan destacados.
- Enlaces apuntan al commit, run y artifact correctos.
- Fallos de Teams se registran y reintentan de forma acotada.
- Payload y logs no contienen secretos ni PII.
- `Reconocer` no altera estados ASVS.
- La conexión tiene propietario y reemplazo documentados.

## Entregables esperados

- Export/solución del flujo según el estándar interno de Power Platform.
- Nombre y propietario del flujo, equipo y canal.
- Variables y conexiones documentadas sin valores secretos.
- Ejemplo de payload y respuesta.
- Export o captura de la Adaptive Card.
- Evidencia de pruebas de aceptación.
- Runbook de rotación, fallos, reintentos y desactivación.
- Gaps y decisiones que requieran ADR.

## Definition of Done

- Funciona en un canal de prueba con un payload real del baseline.
- Autenticación y permisos fueron revisados.
- Deduplicación y severidad funcionan según lo acordado.
- La evidencia solo es accesible por usuarios autorizados.
- Un fallo de Power Automate no falsea el resultado ASVS.
- Operación, ownership y rotación están documentados.
- Un responsable humano aprobó tarjeta y comportamiento.

## Decisiones pendientes

- Equipo, canal y propietarios.
- Autenticación admitida por la organización.
- Destino auditado de respuestas humanas.
- Política de reaviso y retención.
- Si release se bloquea cuando falla solo la notificación.
- Si los hallazgos persistentes requieren además GitHub Issue.

