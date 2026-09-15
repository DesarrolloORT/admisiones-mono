---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-013 — Integración de envío de datos de postulantes al sistema externo "Tivenos"

> **Retrospectivo, confianza `low` — excepción al criterio habitual.** Por protocolo, un candidato de confianza `low` normalmente se conserva solo como investigación, sin redactar ADR. Se redacta este igual, a pedido explícito de avanzar con todos los candidatos, **precisamente porque el bajo nivel de información conocido sobre una integración que mueve datos personales es en sí mismo el riesgo a señalar**. No confirmar, aceptar ni cerrar este ADR sin que el equipo funcional/legal defina qué es Tivenos y su base legal. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-014--integración-de-envío-de-datos-de-postulantes-a-sistema-externo-tivenos-marketingseguimiento-vía-cola) — candidato `ARCH-HIST-014`.

## Estado

`draft` — **no promover a `accepted`**. Este documento existe para dejar visible una integración de datos personales cuyo propósito no se pudo determinar desde el código, no para documentar una decisión entendida.

## Contexto

`api-admisiones` tiene, desde antes del monorepo, una entidad de base de datos `EnvioParaTiveno` (generada por Devart, ver `ADR-007`) pensada para encolar mensajes hacia un sistema externo llamado **"Tivenos"**.

## Lo que sí se sabe (hechos verificados)

- `0d49f9d0` (2026-06-19): implementa `TivenosEnvioService`, que encola datos de interés de productos/carreras hacia Tivenos.
- `0428441e` (mismo día): sincroniza datos de bachillerato del postulante con Tivenos.
- `c59e88fc` (2026-06-25): elimina la lógica condicional de encolado — a partir de acá, el envío es **incondicional**.
- `2eb02a5b` (2026-08-10): se agrega un mensaje adicional, "site registration", cuando una persona registra su primer interés en un producto.
- Documentado técnicamente en `api-admisiones/docs/11-estructura-tivenos-piloto.md`: es una integración por **tabla/cola, no HTTP**; los DTOs no se exponen en ningún endpoint público.

## Lo que no se sabe (y no se debe inventar)

- Qué es "Tivenos" — nombre consistente con una plataforma de marketing/CRM educativo, pero **no confirmado**.
- Qué datos exactos recibe, con qué finalidad, y si hay retención o hay un proceso de baja.
- Si los postulantes fueron informados o dieron consentimiento para este envío.
- Si `EnvioParaTiveno` es leída por un proceso batch propio de ORT o por un tercero externo.

## Por qué esto importa

Los datos enviados (interés académico, datos de bachillerato) son datos personales de postulantes, muchos de ellos menores de edad o recién egresados de secundaria. Enviarlos de forma incondicional a un sistema cuyo propósito no está documentado en este repositorio es, como mínimo, un gap de trazabilidad que el equipo de seguridad/privacidad debería cerrar antes de asumir que el flujo actual es conforme a normativa de protección de datos.

## Gaps y próximos pasos

- Preguntar al equipo funcional/comercial qué es Tivenos y su propósito de negocio.
- Confirmar con legal/DPO la base legal del envío (interés legítimo, consentimiento, u otro) — igual que el gap ya señalado en `ADR-004` para el reconocimiento de documentos.
- Si Tivenos resulta ser, por ejemplo, una herramienta de seguimiento comercial de admisiones, documentar el acuerdo/contrato con ese proveedor si existe.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-014`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-014--integración-de-envío-de-datos-de-postulantes-a-sistema-externo-tivenos-marketingseguimiento-vía-cola)
- Documentación técnica interna: `api-admisiones/docs/11-estructura-tivenos-piloto.md`
- Relacionado: `ADR-004` (mismo tipo de gap de base legal/privacidad, para reconocimiento de documentos vía Azure)
