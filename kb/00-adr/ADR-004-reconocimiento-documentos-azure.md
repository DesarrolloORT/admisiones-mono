---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-004 — Reconocimiento de documentos de identidad vía Azure Document Intelligence + Azure Face

> **Retrospectivo.** Reconstruido a partir de un prototipo frontend (abril 2026) y su productivización en el backend (mayo 2026). Confianza **alta** en la integración y su configuración (explícitas en código/config); confianza más baja en la elección específica de Azure como proveedor. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-008--reconocimiento-de-documentos-de-identidad-vía-azure-document-intelligence--azure-face-prototipado-en-sandbox-frontend-productivizado-en-backend) — candidato `ARCH-HIST-008`.

## Estado

`draft` — pendiente de revisión y validación por el equipo, **en particular por legal/DPO dado el tratamiento de datos personales/biométricos**. No marcar `accepted` sin esa revisión explícita.

## Contexto

El flujo de registro de postulantes requiere verificar la identidad de la persona a partir de un documento (cédula, pasaporte, etc.). En abril de 2026 existía un prototipo exploratorio en el frontend (`sandbox`) que subía el documento y mostraba campos reconocidos, sin integración real confirmada.

## Problema

¿Cómo automatizar la extracción de datos de un documento de identidad y la verificación facial del postulante, de forma productiva (no solo un prototipo)?

## Opciones consideradas

No hay evidencia de alternativas evaluadas (AWS Textract/Rekognition, Google Document AI, OCR propio). El proveedor elegido, Azure, es consistente con el resto de la infraestructura de observabilidad del proyecto (OTLP/Grafana Loki apuntando a `metricasdesa.ort.edu.uy`), lo que sugiere una relación preexistente con Azure/infraestructura de ORT, pero esto no está confirmado como motivo.

## Decisión

Integrar **Azure Document Intelligence** (modelos `prebuilt-idDocument` y `prebuilt-read`) y **Azure Face** (`detection_03`) desde `api-admisiones`, vía un nuevo `AzureService` (submódulo `Core`) y el endpoint `POST /AnalizarAdjunto`:

1. `4af236ef` (2026-05-18): agrega `AzureService` y el endpoint de análisis.
2. `90ceb97f` (mismo día): configura Document Intelligence y Face, con `DeleteAnalyzeResult: true`.
3. `975e3d8a` (mismo día): rate limiting (5 req/min) y límite de tamaño de archivo (2MB) para el endpoint.

## Consecuencias

- Se envían documentos de identidad y datos biométricos faciales de postulantes a un servicio cloud de terceros (Microsoft Azure). `DeleteAnalyzeResult: true` limita la retención del resultado de análisis en Azure, pero esto no está documentado como una decisión consciente de minimización de datos — se infiere de la configuración.
- Rate limiting y límite de tamaño aplicados desde el inicio de la integración.
- Reemplaza al prototipo `sandbox` del frontend (`ARCH-HIST-008` inicial, `33ea7abc`) — confirmar con el equipo si ese sandbox se retiró o sigue como herramienta de prueba interna.

## Gaps

- **Sin base legal ni evaluación de impacto de privacidad (DPIA) documentada** para el procesamiento de datos biométricos/documentos de identidad de postulantes. Consultar con legal/DPO antes de aceptar este ADR — no asumir cumplimiento.
- Motivo de elección de Azure como proveedor, no confirmado.
- Ver también la nota sobre `ecea9100` (columna `HashTokenPassword` agregada a `t_persona` sin migración visible) en `ARCH-HIST-003`/`ARCH-HIST-008` del documento de descubrimiento: confirma que el esquema de base de datos se gestiona fuera de este repositorio.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-008`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-008--reconocimiento-de-documentos-de-identidad-vía-azure-document-intelligence--azure-face-prototipado-en-sandbox-frontend-productivizado-en-backend)
- Commits fuente: `33ea7abc8a202559775b9d61d5d74a46dff5798a` (prototipo frontend), `4af236ef04e0ff4fce13f80e536b7de4518ed89e`, `90ceb97f17de9b114da89287b44a0cabadf6c295`, `975e3d8ad26b55f9d4ab053401edacecd94b52b6`, `56f45263`
- Relacionado: `ARCH-HIST-003` (persistencia vía Devart, gap de gobernanza de esquema de base de datos)
