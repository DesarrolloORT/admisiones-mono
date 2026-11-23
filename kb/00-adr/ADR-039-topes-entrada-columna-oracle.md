---
status: accepted
owner: gabriele
updated: 2026-11-13
---

# ADR-039 — Topes de entrada: largo de la columna de Oracle y cantidad de filas antes de cargar

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entrada del 2026-11-13 11:07 (SEG-L3-01, SEG-L3-02, SEG-L3-04).

## Estado

`accepted` (2026-11-13).

## Contexto

- Pasarse del largo de la columna daba 500 por `ORA-12899`. En el registro, además, se guardaban pendientes de hasta 16 MB en Redis.
- El PUT y el confirm de la DJ cargaban todo el árbol, con los BLOB de los comprobantes, antes de mirar el estado. Repetir el PUT sobre una DJ confirmada cargaba N×10 MB para devolver 409.
- `RedisCacheService.GetOrSetAsync` reejecutaba la consulta cuando fallaba y no reescribía una entrada con JSON corrupto.

## Decisión

- **`[MaxLength]` igual a la columna de Oracle** en registro, login, `PUT person/details`, DJ y encuesta inicial. El tipo y el número de documento tienen margen (10 y 30) para espacios y puntos antes de normalizar ([ADR-037](ADR-037-cedula-forma-canonica.md)). Pasarse responde 400 con el mensaje genérico de validación.
- **En una lista, el tope se valida en el validador**, porque `[MaxLength]` cuenta los elementos. Las listas llevan hasta 20 elementos y cada nombre de "otra universidad" hasta 90 caracteres.
- **DJ: chequear antes de cargar.**
  1. Se lee la cabecera sin los comprobantes.
  2. Se chequean el estado y el plazo.
  3. Recién ahí se carga el árbol completo.

  Topes técnicos: 20 integrantes, 12 ingresos por integrante y 50 gastos (400 `Affidavit.TooManyRows`), en `AffidavitSaveValidation`.
- **Caché:** solo lo que toca Redis va dentro del try. La consulta corre una sola vez y su excepción se propaga. La escritura siempre pisa la entrada.

## Consecuencias

- `notes` (observaciones de la DJ) va a una columna de 20 caracteres que el front no limita. Antes, pasarse daba 500; ahora da 400. Hay que limitarlo en pantalla o revisar la columna.
- Si negocio quiere otros topes de filas, se cambian en un solo lugar.

## Referencias

- [ADR-017](ADR-017-result-t-formato-unico-error.md): formato de error.
- [ADR-026](ADR-026-becas-dj-por-fondo.md): DJ.
