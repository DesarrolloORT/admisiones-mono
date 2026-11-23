---
status: accepted
owner: gabriele
updated: 2026-11-18
---

# ADR-038 — Archivos subidos: el tipo sale del contenido y la estructura se valida sin decodificar

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-11-13 10:11 (SEG-L2-05), 2026-11-13 11:07 (SEG-L3-03), 2026-11-17 10:27 (SEG-L6-03) y 2026-11-18 11:39 (PT-L2).

## Estado

`accepted` (2026-11-18).

## Contexto

`FileValidator` ([ADR-019](ADR-019-uploads-multipart.md), [ADR-020](ADR-020-core-a-packages-ort.md)) ya validaba los magic bytes, pero:

- la extensión guardada salía del nombre que mandaba el cliente: una foto podía quedar `.html` y `recibo.pdf` con un PNG adentro se descargaba como PDF;
- una firma válida seguida de basura pasaba;
- los uploads se leían hasta los 16 MB globales;
- la caché de imágenes aceptaba un PDF que después hacía fallar la activación.

## Opciones consideradas

- **Decodificar con SkiaSharp:** dependencia nativa de unos 10 MB en la imagen Docker y CPU por cada subida, para un hallazgo bajo. Se descarta.
- **Validar la estructura del contenedor sin decodificar:** es la opción elegida.

## Decisión

- **El tipo lo da el contenido.** `FileValidator.DetectExtension` / `DetectImageExtension` / `ContentTypeFor` lo toman de los primeros bytes.
  - Foto de la persona: `.jpg` o `.png` según el contenido, y `GET person/photo` responde el `Content-Type` real.
  - Adjuntos de la DJ: se guarda la extensión del contenido.
  - **Documento de identidad: siempre `.jpg`.** Otros sistemas de ORT leen esas filas por el nombre (README de Identity).
- **Estructura (`FILE_VAL_08`):**
  - PNG: cadena de bloques desde IHDR hasta IEND exacto, sin CRC.
  - JPEG: segmentos válidos hasta SOS y EOI. Lo que venga después del EOI se acepta, porque Samsung agrega metadatos y las fotos en movimiento de Pixel agregan un video.
- **Tope por endpoint:** `[RequestSizeLimit]` igual al máximo de `FileValidator` más 1 MB (11 MB los documentos, 6 MB la foto). Kestrel corta antes de bufferizar, y la API responde 413 `Request.TooLarge`.
- **La caché de imágenes solo guarda JPG/PNG.** El OCR sigue aceptando PDF.

## Consecuencias

- Un JPEG con algo pegado al final sigue entrando. El riesgo es bajo porque se sirve como imagen con `nosniff`.
- **Afuera:** quitar EXIF/GPS. Requiere volver a codificar y sumar ImageSharp como dependencia directa ([ADR-042](ADR-042-cadena-suministro-nuget.md)).
- **Falta:** probar en develop una foto real de un celular Samsung o Pixel.

## Referencias

- [ADR-022](ADR-022-imagenes-documento-redis-cifradas.md): caché de imágenes.
