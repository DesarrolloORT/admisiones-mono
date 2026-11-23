---
status: accepted
owner: gabriele
updated: 2026-11-17
---

# ADR-042 — Cadena de suministro NuGet: fuentes mapeadas, pines en `Directory.Build.props` e ImageSharp 3.1.11 como riesgo aceptado

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-11-13 08:43 (SEG-L1-06), 2026-11-13 09:40 (SEG-DEP-01) y 2026-11-17 12:13 (SEG-DEP-02).

## Estado

`accepted` (2026-11-17). ImageSharp 3.1.11 queda como **riesgo aceptado temporal**.

## Contexto

- **TRV-14:** `nuget.config` mezclaba GITORT y nuget.org sin mapeo, lo que abre la puerta a *dependency confusion* con paquetes `ORT.*`.
- **TRV-24:** había tres paquetes de Serilog en versión `-dev`.
- `dotnet list package --vulnerable` marcaba `System.Security.Cryptography.Xml`/`Pkcs` 10.0.10 (DoS, altas) y `SixLabors.ImageSharp` 3.1.11, que llega transitivo de `ORT.Vision.Azure` 2.0.9 y es alcanzable desde el endpoint anónimo `analyze-attachment`.

## Decisión

- **`nuget.config`** con `<clear/>` y `packageSourceMapping`: `ORT.*` solo desde GITORT y el resto solo desde nuget.org. No se agrega lockfile porque el mapeo ya corta el vector.
- **Pines transitivos en `WebApiAdmisiones/Directory.Build.props`:** Crypto.Xml y Crypto.Pkcs 10.0.14. Se eligió la 10.0.14 y no la 10.0.15 porque esta tenía 3 días publicada.
- **El Dockerfile copia `Directory.Build.props`.** Antes no lo copiaba, así que ningún pin llegaba a las imágenes publicadas.
- **ImageSharp vuelve a 3.1.11.** La 3.2.0 exige una licencia de Six Labors en los builds Release, y ORT no la va a tener. Los CVE de TIFF no son alcanzables porque `analyze-attachment` solo deja pasar JPG, PNG y PDF por magic bytes. Siguen abiertos el de perfiles ICC y el de `HistogramEqualization`, que no se pudo verificar si `ORT.Vision.Azure` usa.
- Serilog pasa a las versiones estables más cercanas.

## Consecuencias

- **Arreglo de fondo (dueño de `ORT.Vision.Azure`):** reemplazar ImageSharp por una librería sin licencia comercial, como SkiaSharp o Magick.NET, y publicar una versión nueva.
- `Core/DbConnectionContext` sigue declarando Pkcs 8.0.1 y el escaneo lo marca, pero no llega al binario. Se arregla en el repo de Core.
- Verificar con `dotnet publish -c Release`, no en Debug: el target de licencia solo corta el build en Release.

## Referencias

- [ADR-020](ADR-020-core-a-packages-ort.md): packages `ORT.*`.
- [ADR-038](ADR-038-archivos-tipo-por-contenido.md): validación de archivos antes del decoder.
