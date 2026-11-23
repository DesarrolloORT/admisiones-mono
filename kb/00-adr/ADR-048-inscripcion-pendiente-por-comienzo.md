---
status: accepted
owner: gabriele
updated: 2026-11-16
---

# ADR-048 — Inscripción pendiente: en nivel 3 y 4 bloquea por producto y comienzo

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entrada del 2026-11-16 15:45.

## Estado

`accepted` (2026-11-16). El cambio vive en LogicaORT (rama `gabriele/confirmarpreinscripcion`), no en este monorepo.

## Contexto

Reactivar una inscripción de nivel 4 (por ejemplo, Certificado en Gerencia) devolvía 400 `CodigoError 381`, "Ya tiene una inscripción pendiente al producto indicado". `ConfirmarPreInscripcionMultiple` rechazaba si había cualquier trámite abierto (procesos 75/82) para el producto, sin mirar el comienzo.

## Decisión

- En nivel 3 y 4 se admite **un trámite pendiente por producto y comienzo**. Con Octubre a la espera, se puede confirmar Noviembre del mismo producto.
- `InstanciaWorkFlow.SetWhereInscripcionesPendientes(persona, producto, idsComienzo)` suma `id_comienzo IN (...)`.
- El chequeo corre después de resolver el comienzo real de cada oferta.
- Nivel 1 y 2 siguen bloqueando por producto.

## Consecuencias

- `ConfirmarPreInscripcion` (simple) y `ActualizarInteres` no cambian: la API nueva no los llama.
- **Inconsistencias abiertas, sin tocar:**
  - esta API usa los procesos 75/89 en `TieneInscripcionPendienteParaProducto` y LogicaORT usa 75/82;
  - la instancia corporativa (proceso 89) guarda `IdObjetoInstanciaWorkflow = IdOferta`, pero el filtro busca la persona.
- En el front, la track key del `@for` de tarjetas se duplica (`NG0955`).

## Referencias

- [ADR-003](ADR-003-integracion-servicio-inscripciones-pagos.md): Inscripciones y Pagos.
