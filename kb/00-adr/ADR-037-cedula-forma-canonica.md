---
status: accepted
owner: gabriele
updated: 2026-11-13
---

# ADR-037 — La cédula se normaliza a `NNNNNNN-D` al entrar; el número es único entre tipos

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entrada del 2026-11-13 10:11 (SEG-L2-01, SEG-L2-02).

## Estado

`accepted` (2026-11-13).

## Contexto

`CedulaUruguaya.Validate` toleraba puntos y espacios, pero la búsqueda y el guardado usaban el texto tal cual llegaba. `1.234.567-2` pasaba la validación, no encontraba a la persona guardada como `1234567-2` y dejaba crear una segunda persona con la misma CI. Además, `PUT person/details` no validaba el documento nuevo. Con tipo `PS` y la cédula de otra persona, se salteaba el chequeo de unicidad por (tipo, número).

## Decisión

- **`IdentityDocumentRules.CanonicalDocument`** deja la CI en `NNNNNNN-D`, la forma que manda el front (`formatDocumentForBackend`) y la que usa el legacy.
  - Saca solo puntos, espacios y guiones, y conserva los ceros a la izquierda.
  - Si queda algo que no es dígito, devuelve la entrada tal cual y la validación la rechaza como antes. Normalizar no vuelve válida una entrada inválida.
  - Los otros tipos de documento solo se recortan.
- Se aplica al entrar en registro, verificación, login, recupero y `PUT person/details`. La sesión del registro guarda la forma canónica.
- **Unicidad en `PUT person/details`:** si cambia el **número**, no puede pertenecer a otra persona con ningún tipo, porque registro, verificación y recupero buscan por número sin tipo. Si solo cambia el tipo, se chequea por (tipo, número). Un tipo fuera de CI/DE/PS/CC da 400.

## Consecuencias

- Efecto colateral buscado: `analyze-attachment` ahora encuentra a la persona por el número compacto del OCR, y se le aplica el TTL de 24 h de [ADR-022](ADR-022-imagenes-documento-redis-cifradas.md).
- **Afuera:** normalizar las filas viejas de `T_PERSONA` con otro formato. Requiere relevar la base.
- Tests de propiedades con FsCheck (`CedulaUruguayaPropertyTests`): los dos formatos aceptan y rechazan lo mismo, y `Normalize` es idempotente.

## Referencias

- [ADR-010](ADR-010-login-tipodocumento-recuperacion-jwt.md): login por documento.
- [ADR-029](ADR-029-registro-datos-no-coinciden-solicitud-alta.md): registro.
