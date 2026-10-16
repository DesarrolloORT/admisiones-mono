---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-019 — Subida de archivos por `multipart/form-data`

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entrada del 2026-09-18.

## Estado

`accepted` (2026-09-18).

## Contexto

Los uploads viajaban como JSON con el archivo en base64. El WAF bloqueó el comprobante de ingreso de la DJ: un `POST` con un blob base64 grande en el body es el patrón que inspecciona. Además, base64 infla el payload un 33 %.

## Problema

¿Cómo subir archivos sin que el WAF los bloquee?

## Opciones consideradas

- **Migrar solo el endpoint reportado.** Descartada: dejaba cuatro endpoints con el mismo problema y dos formas de subir archivos.
- **Aceptar JSON y multipart a la vez.** Descartada: mientras el front mande JSON, el WAF sigue viendo el payload gordo, y el camino base64 se queda para siempre.
- **Los cinco endpoints a multipart, sin convivencia.** Elegida.

## Decisión

- `income-receipt`, `affidavit-document`, `person/photo`, `person/identity-document` y `registration/analyze-attachment` pasan a `[FromForm]` con `IFormFile`. Las rutas no cambian.
- El cambio queda en el binding; por debajo del controller todo sigue con `byte[]` + `fileName`.
- `JsonSchemaValidationFilter` (global) acepta también `multipart/form-data` y valida esquema solo si el body es JSON. Cada acción declara su `[Consumes]`.
- `MaxRequestBodySize` se queda en 16 MB.

## Consecuencias

- **Breaking:** el JSON viejo devuelve 415. Front y back se despliegan juntos.
- `identity-document` con frente y dorso de 10 MB cada uno no entra (tampoco entraba antes).
- Falta la prueba de ida y vuelta contra Oracle con un PDF real por endpoint.

## Referencias

- [ADR-009](ADR-009-hardening-seguridad-api-publica.md)
