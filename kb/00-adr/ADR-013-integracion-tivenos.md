---
status: draft
owner: rubino-f
updated: 2026-09-17
---

# ADR-013 — Integración de envío de datos de postulantes al sistema externo "Tivenos"

> **Retrospectivo, confianza `low` — excepción al criterio habitual.** Por protocolo, un candidato de confianza `low` normalmente se conserva solo como investigación, sin redactar ADR. Se redacta este igual, a pedido explícito de avanzar con todos los candidatos, **precisamente porque el bajo nivel de información conocido sobre una integración que mueve datos personales es en sí mismo el riesgo a señalar**. No confirmar, aceptar ni cerrar este ADR sin que el equipo funcional/legal defina qué es Tivenos y su base legal. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-014--integración-de-envío-de-datos-de-postulantes-a-sistema-externo-tivenos-marketingseguimiento-vía-cola) — candidato `ARCH-HIST-014`.

## Estado

`draft` — **no promover a `accepted`**. Este documento existe para dejar visible una integración de datos personales cuyo propósito no se pudo determinar desde el código, no para documentar una decisión entendida.

Revisado el 2026-09-17: el equipo confirmó qué es Tivenos (ver abajo). **Eso no desbloquea el ADR: lo agrava.** Saber que es un proveedor externo usado con fines de marketing convierte el gap de "no sabemos a dónde van estos datos" en "sabemos que hay una transferencia de datos personales a un tercero con finalidad comercial, sin base legal documentada". Sigue bloqueado a la espera de legal/DPO.

## Contexto

`api-admisiones` tiene, desde antes del monorepo, una entidad de base de datos `EnvioParaTiveno` (generada por Devart, ver `ADR-007`) pensada para encolar mensajes hacia un sistema externo llamado **"Tivenos"**.

## Lo que sí se sabe (hechos verificados)

- `0d49f9d0` (2026-06-19): implementa `TivenosEnvioService`, que encola datos de interés de productos/carreras hacia Tivenos.
- `0428441e` (mismo día): sincroniza datos de bachillerato del postulante con Tivenos.
- `c59e88fc` (2026-06-25): elimina la lógica condicional de encolado — a partir de acá, el envío es **incondicional**.
- `2eb02a5b` (2026-08-10): se agrega un mensaje adicional, "site registration", cuando una persona registra su primer interés en un producto.
- Documentado técnicamente en `api-admisiones/docs/11-estructura-tivenos-piloto.md`: es una integración por **tabla/cola, no HTTP**; los DTOs no se exponen en ningún endpoint público.

## Qué es Tivenos (confirmado por el equipo, 2026-09-17)

**Un proveedor externo a ORT, usado con fines de marketing / seguimiento comercial de postulantes.** Es decir: los datos no quedan dentro de la organización, hay una transferencia a un tercero que actúa como encargado del tratamiento.

Esto confirma la hipótesis que el ADR marcaba como no verificada, y sube el nivel de riesgo en dos sentidos:

1. **Es una transferencia a un tercero**, no un movimiento interno de datos. Requiere contrato de encargo de tratamiento, y garantías sobre retención, subencargados y ubicación de los datos.
2. **La finalidad es comercial, no la prestación del servicio de admisión.** Un postulante que se registra para inscribirse no está, por ese solo hecho, consintiendo el uso de sus datos para marketing. La base legal del envío no puede darse por cubierta con la del registro.

Agravante ya registrado: desde `c59e88fc` (2026-06-25) **el envío es incondicional** — no hay opt-in, opt-out ni condición que lo module. Todo postulante que registra interés genera envío.

## Lo que sigue sin saberse

- Qué datos exactos recibe el proveedor, con qué finalidad específica, y si hay política de retención o proceso de baja.
- Si los postulantes fueron informados o dieron consentimiento para este envío.
- Cómo llega la cola `EnvioParaTiveno` al proveedor: qué proceso la lee, con qué credenciales y por qué canal. Dado que el destino es externo, esto es parte de la superficie de exposición de datos y debería estar documentado.

## Por qué esto importa

Los datos enviados (interés académico, datos de bachillerato) son datos personales de postulantes, muchos de ellos menores de edad o recién egresados de secundaria. Enviarlos de forma incondicional a un sistema cuyo propósito no está documentado en este repositorio es, como mínimo, un gap de trazabilidad que el equipo de seguridad/privacidad debería cerrar antes de asumir que el flujo actual es conforme a normativa de protección de datos.

## Próximos pasos para desbloquear

1. **Base legal del envío** (legal/DPO): consentimiento específico para fines comerciales, o interés legítimo con su balancing test documentado. No alcanza con la base legal del registro de admisión — la finalidad es distinta. Mismo bloqueo que `ADR-004`.
2. **Contrato de encargo de tratamiento con el proveedor**: confirmar si existe y dónde vive, con sus cláusulas de retención, subencargados y ubicación de los datos.
3. **Revisar la incondicionalidad del envío**: si la base legal termina siendo el consentimiento, el envío incondicional de `c59e88fc` es incompatible y hay que reintroducir una condición.
4. **Documentar el canal de salida**: qué proceso lee `EnvioParaTiveno` y cómo llega al proveedor.

Mientras tanto, este ADR permanece en `draft` como registro visible del riesgo.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-014`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-014--integración-de-envío-de-datos-de-postulantes-a-sistema-externo-tivenos-marketingseguimiento-vía-cola)
- Documentación técnica interna: `api-admisiones/docs/11-estructura-tivenos-piloto.md`
- Relacionado: `ADR-004` (mismo tipo de gap de base legal/privacidad, para reconocimiento de documentos vía Azure)
