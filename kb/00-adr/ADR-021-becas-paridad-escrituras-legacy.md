---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-021 — Becas: las escrituras replican al legacy, salvo donde cambió la regla

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-01 al 2026-10-07.

## Estado

`accepted` (2026-10-06).

## Contexto

La postulación a beca y la declaración jurada (DJ) se reescribieron sobre las mismas tablas que usa el sitio viejo (LogicaORT). Los consumidores de esas columnas están afuera del repo: SGE3, el backoffice de la comisión y el proceso que lee `T_ENVIO_PARA_TIVENOS`. No se puede verificar qué leen.

## Problema

Cuando el API nuevo escribe distinto que el legacy, ¿cuál manda?

## Decisión

**Por defecto, igual al legacy.** Igualar evita depender de confirmaciones que nadie puede dar. Se aplicó a:

- PDF de la DJ (`PDF_DECJURADA`): plantilla copiada por script, mismas convenciones de `Broker.ObtenerValor`.
- Mensaje `Beca_CompletaDJ` a Tivenos: `Disparador = AltaPostulacionBeca`, `OrigenLlamador` y `ProductoId` en NULL.
- Nombre del adjunto con extensión, y la extensión aparte.
- `CAN_FAM_CURSA_INSCRIPTO_PRUEBA` en 0; FEXA (33) sin núcleo familiar.
- Prueba no encontrada → 409, como el 1203 del legacy.
- Usuario `ADMISIONES` en `T_ACTIVIDAD`/`T_ACCION`.

**Diferencias deliberadas:**

| Tema | Legacy | Nuevo | Por qué |
|---|---|---|---|
| Inscripción | se podía postular sin inscripción | sin inscripción → 409 | regla nueva |
| `ModificacionInteres` | grado 5 y desinterés de hermanos | no se corre | con inscripción obligatoria no aplica; ya no se desinteresa a nadie |
| Comprobante sin subir | `empty_blob()` | NULL | nadie distingue NULL de BLOB vacío |
| Eliminar adjunto | deja el BLOB | borra el BLOB | bug del legacy |
| Extensiones | también Office y HTML | PDF/JPG/PNG | diseño |
| Fallo del PDF | error con la DJ ya confirmada | se loguea, responde éxito | |
| `WARNING_DECLARACION_JURADA` | nunca llegaba a la base | se persiste | |

## Consecuencias

- Si el usuario de la conexión Oracle cambia en algún ambiente, cambia lo que ve el CRM.
- SGE3 y la comisión van a ver warnings que antes no veían.
- Pendiente de negocio: "Descuentos legales" imprime `$ -100000`; Liberation Sans vs Arial en el contenedor; extensiones de adjuntos.
- Sin comparar todavía: las lecturas del paso 1 y "Mis becas" contra el sitio viejo.
- Un mensaje propio del API nuevo a Tivenos requiere pactarlo con quien mantiene ese proceso.

## Referencias

- `api-admisiones/docs/BECAS-DECLARACION-JURADA.md`
- [ADR-013](ADR-013-integracion-tivenos.md) — Tivenos
- [ADR-025](ADR-025-becas-inscripcion-obligatoria.md), [ADR-026](ADR-026-becas-dj-por-fondo.md), [ADR-027](ADR-027-becas-pdf-dj.md), [ADR-028](ADR-028-becas-flujo-postulacion.md) — detalle de las diferencias y del flujo
