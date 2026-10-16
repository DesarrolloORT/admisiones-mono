---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-027 — Becas: el PDF de la DJ se genera en la API con la plantilla del legacy

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-06 09:30 y 2026-10-09 12:25.

## Estado

`accepted` (2026-10-06). Revierte la decisión anterior de no generar `PDF_DECJURADA`.

## Contexto

Nadie en LogicaORT ni en este repo lee `PDF_DECJURADA`; el consumidor está afuera (SGE3 / backoffice de la comisión). Pedido explícito: tiene que quedar igual que en el sistema viejo.

## Decisión

- La plantilla (`FormadorPdf.CrearPdfDeclaracionJurada`, MigraDoc 1.32) se **copia por script**, no a mano: solo se renombran tipos y se saca el código comentado. MigraDoc 6 (`PDFsharp-MigraDoc`, MIT) la compila sin cambios.
- `PdfDeclaracion` replica los objetos del legacy y las convenciones de `Broker.ObtenerValor` (nulo = -100000, "SI"/"S" = verdadero, fecha nula = `MinValue`) y el orden de las consultas, porque la plantilla depende de ellas.
- Números con coma decimal mediante una cultura clonada: la API corre con globalización invariante.
- Se genera **después del commit**. Si falla, se loguea y la respuesta sigue siendo éxito (el legacy devolvía error con la DJ ya confirmada).
- Render bajo **lock global**: con renders en paralelo, PDFsharp mezcla glifos entre documentos.
- Contenedor con `fonts-liberation` (mismos anchos que Arial).
- El mail de confirmación se dio de baja; el PDF se mantiene ([ADR-018](ADR-018-mails-service-bus-contratos-tipados.md)).

## Consecuencias

- El lock global serializa los renders; alcanza para el volumen de confirmaciones.
- Una DJ puede quedar confirmada sin PDF; solo lo delata el log.
- Pendiente de negocio: "Descuentos legales" imprime `$ -100000` (la columna queda NULL desde el rediseño); validar contra un `PDF_DECJURADA` real; Liberation Sans o Arial real (`ttf-mscorefonts-installer`).

## Referencias

- [ADR-021](ADR-021-becas-paridad-escrituras-legacy.md) — paridad con el legacy
