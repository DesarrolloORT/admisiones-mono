---
status: accepted
owner: gabriele
updated: 2026-11-18
---

# ADR-041 — Datos personales en logs: `[Redact]`, huella del documento y un test que lo exige

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-11-13 08:43 (SEG-L1-07) y 2026-11-18 09:34 (PT-M3).

## Estado

`accepted` (2026-11-18).

## Contexto

El log de requests serializaba los DTOs completos: cédula, apellido, mail, DJ y nombres de archivo. El pentest interno (PT-M3) encontró la cédula en claro en Loki.

## Decisión

- **`[Redact]`** vale en una propiedad o en una clase entera. En una clase, el objeto se loguea como `<redacted:Tipo>`. Se aplica a los requests de registro, verificación, `person/details`, DJ, consulta de beca, login, recupero y a los `IFormFile`.
- **`LogRedaction.Document()`** (AppLogic.Contracts) genera una huella `doc#xxxxxxxx` del documento normalizado. Es estable entre formatos y permite correlacionar sin la cédula. Se usa en login, registro y en la caché de imágenes.
- **`RequestPiiRedactionTests`** recorre por reflexión los parámetros de todas las acciones, incluidos los anidados. Falla si una propiedad con nombre de dato personal no tiene `[Redact]`. Los `*Id` se excluyen a propósito.

## Consecuencias

- Un DTO nuevo con un dato personal sin marcar rompe el build de tests.
- **Pendiente:** las claves del rate limiter de login todavía llevan el documento normalizado, y se loguean al rechazar. Cambiarlas resetea los contadores en el deploy.
- **Afuera (ENR-12):** el body de error de la API interna sigue en el log. Es la única pista para diagnosticar sus rechazos.

## Referencias

- [ADR-004](ADR-004-reconocimiento-documentos-azure.md): tratamiento de datos personales.
- [ADR-035](ADR-035-ip-cliente-proxies-confianza.md): IP en logs.
