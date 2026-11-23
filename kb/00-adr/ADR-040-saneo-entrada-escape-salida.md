---
status: accepted
owner: gabriele
updated: 2026-11-17
---

# ADR-040 — Datos hacia otros sistemas: se mantiene el saneo en la entrada y se escapa al armar XML

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entrada del 2026-11-17 10:27 (SEG-L6-01, SEG-L6-02).

## Estado

`accepted` (2026-11-17).

## Contexto

Lo que guarda la API lo leen sistemas ajenos al repo, como el backoffice y la bandeja de workflow. Había dos huecos:

- **SEG-L6-01:** `BuildCorporateInstanceXml` pegaba nombre, apellido, comienzo, producto y turno sin escapar. El nombre lo edita la persona, así que una comilla permitía inyectar atributos o etiquetas en el XML que lee la bandeja.
- **SEG-L6-02:** `SanitizeAttribute` trataba una `List<T>` como un objeto y nunca saneaba sus elementos, como los integrantes y gastos de la DJ o las "otras universidades". Eso podía terminar en XSS almacenado en el backoffice.

## Problema

Lo correcto es codificar en la salida de cada consumidor, pero esos consumidores no están en este repo.

## Decisión

- **Se mantiene el saneo en la entrada** y ahora recorre `IList`: los strings se sanean en su lugar y los objetos como hasta ahora. No se cambia de enfoque porque depende de cómo renderiza el backoffice, que no se puede verificar desde acá.
- **Lo que la API arma, lo escapa:** cada valor del XML corporativo pasa por `SecurityElement.Escape`. El formato no cambia, incluido el espacio inicial de `data`.

## Consecuencias

- Un `<` o un `&` quedan codificados en la base, igual que en los campos sueltos.
- Si el backoffice pasa a codificar en la salida, el saneo en la entrada se puede revisar con quien lo mantiene.

## Referencias

- [ADR-021](ADR-021-becas-paridad-escrituras-legacy.md): consumidores externos de las tablas de becas.
