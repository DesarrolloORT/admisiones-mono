---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-017 — `Result<T>` en los casos de uso y un único formato de error HTTP

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-02 al 2026-10-06 (plan `docs/PLAN-MIGRACION-RESULT.md`).

## Estado

`accepted` (2026-10-05). Reemplaza la forma de claves de [ADR-016](ADR-016-mensajes-usuario-resx.md).

## Contexto

Los casos de uso devolvían `OperationResult<T>` (de `Core`), que mezcla el resultado con el transporte HTTP: código numérico, `methodName` y un envelope. Había catálogos de rechazo (`*Rejection`) y códigos opacos (`FDB_GDJ_44`).

## Problema

¿Cómo sacar el transporte HTTP de `AppLogic` y dejar un solo formato de error hacia el front?

## Decisión

- `ORT.Utilities.Common.Results.Result<T>` reemplaza a `OperationResult<T>`. Los validadores que solo chequean devuelven `Error?` (`null` = pasa); lo que produce un valor devuelve `Result<T>`.
- El tipo de error (`Validation`, `NotFound`, `Conflict`, `Unexpected`, `TooManyRequests`) decide el status; los números HTTP en `AppLogic` quedan solo en los bordes (Core y API de Inscripciones y Pagos).
- Claves `<Area>.<Motivo>` (`Affidavit.HousingTypeRequired`) en lugar de códigos opacos. **Una clave por texto**: los duplicados se unifican porque el código ya no viaja en el error HTTP.
- `ErrorResponseWriter` es el único que arma un error HTTP: status + `{ "message": "..." }`, en controllers, filtros, middlewares y rate limiter.
- Sin datos → 204 sin body. Descargas → archivo binario, no base64 en JSON.
- 401 sin body = sesión inválida; 401 con body = rechazo con mensaje. 401/403 del proveedor interno → `Unexpected` (son credenciales de servicio, no la sesión).
- Errores de LDAP, Azure y del proveedor van al log; el usuario recibe un mensaje genérico localizado.
- Se borran catálogos de rechazo, puentes `ToLegacy` y `OperationResultExtensions`. `OperationResult` queda solo en el borde con `Core` (`ToResult`/`ToError`).

## Consecuencias

- **Breaking con el front**, documentado en `docs/CONTRATO-RESULT-FRONT.md`, los `*.contract.json` y `MATRIZ-TRAZABILIDAD.md`.
- Se cierran las fugas de `ex.Message` de LDAP y del cuerpo del proveedor que listaba la auditoría.
- Balance desde el piloto: AppLogic +2218 / −5407 líneas.
- Las implícitas de `Result<T>` no aplican desde interfaces: todo `IEnumerable<T>` termina en `.ToList()`.
- Los registros históricos conservan los códigos viejos; no hay tabla de equivalencias (la API no estaba en producción).

## Referencias

- `api-admisiones/docs/PLAN-MIGRACION-RESULT.md`, `docs/CONTRATO-RESULT-FRONT.md`, `docs/GUIA-ESTILO-CODIGO.md`
