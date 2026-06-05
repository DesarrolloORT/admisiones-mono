# Manejo de errores con `@desarrolloort/ngx-utils`

## Contrato esperado

La libreria interna debe mantener el manejo de errores HTTP practicamente automatico para las apps consumidoras.

La regla de consumo es:

- En respuestas exitosas, la app trabaja con `data`.
- En errores, la app trabaja con `NormalizedApiError`.

Eso deja a `OperationResult<T>` como un detalle del contrato backend, no como algo que cada feature tenga que parsear.

## Camino feliz

Cuando el backend responde correctamente:

```ts
{
  success: true,
  httpCode: 200,
  message: null,
  data: { id: 1 }
}
```

la app deberia consumir:

```ts
{
  id: 1;
}
```

En esta app, `ApiHttpClient` activa `unwrapOperationResultContext()` por defecto para sus requests. Por eso los adapters de feature reciben directamente `data`.

## Camino de error

Cuando el backend responde con error HTTP o con `OperationResult.success === false`, incluso si vino con HTTP `2xx`, la app deberia recibir un `NormalizedApiError`:

```ts
{
  status: 409,
  errorCode: 'USER_EXISTS',
  method: 'POST /Registro/EvaluarDocumento',
  message: 'Ya existe un usuario',
  data: null,
  action: 'notify',
  isOperationResult: true,
  originalError: ...
}
```

Esto es importante porque la pantalla no solo necesita saber que hubo error. A veces necesita decidir UI o flujo segun `status`, `errorCode`, `method` o `data`.

## Cambios ya cubiertos

La version librería `ngx-utils` ya cubre los puntos importantes:

- `success: false` en `OperationResult` se trata como error real aunque llegue con HTTP `2xx`.
- `ortApiErrorInterceptor` reemite `NormalizedApiError`, no el `HttpErrorResponse` crudo.
- `ApiErrorHandlerService.normalize(...)` permite normalizar sin `notify`, log ni debounce.
- Existen contextos por request:
  - `SUPPRESS_GLOBAL_ERROR`
  - `CUSTOM_ERROR_MESSAGE`
  - `CUSTOM_ERROR_MESSAGES`
  - `CUSTOM_ERROR_POLICY`
  - `UNWRAP_OPERATION_RESULT`
- `unwrapOperationResultContext()` permite activar unwrap por request.
- `unwrapOperationResult` queda default `false` a nivel global.
- `throwOperationResultErrors` ya no forma parte del config.

## Decision en esta app

No activamos `unwrapOperationResult` globalmente. En su lugar, `ApiHttpClient` lo activa por request.

Motivo:

- evita cambiar el comportamiento de cualquier uso directo de `HttpClient`;
- mantiene el unwrap automatico para el camino normal de API de la app;
- concentra la decision en una sola capa.

Los endpoints de feature no deberian crear errores propios para HTTP. Errores como `AuthRequestError`, `CatalogRequestError` o `DocumentRecognitionRequestError` duplican responsabilidad de la libreria y no deberian volver.

Las pantallas pueden leer errores asi:

```ts
if (isNormalizedApiError(error)) {
  this.error.set(error.message);
}
```

Eso no duplica el snackbar global porque no vuelve a llamar al handler de la libreria; solo usa el error ya normalizado que reemitio el interceptor.

## Ajustes que todavia conviene evaluar

1. Tipado de clientes generados

Idealmente el tipo del cliente deberia reflejar que `OperationResult<T>` emite `T` en el camino feliz. Hoy esta app lo resuelve en `ApiHttpClient`, pero podria vivir en una abstraccion compartida si otras apps repiten el mismo patron.

2. `isOperationResult` y `httpCode`

Los modelos generados marcan `httpCode` como opcional. Si el backend puede omitirlo en respuestas exitosas, conviene que la libreria detecte `OperationResult` por `success` + `data`, no solo por `httpCode`.

3. Politicas por `errorCode`

Hoy la politica principal es por status. Para reglas funcionales podria ser util poder decidir por `errorCode`, por ejemplo `USER_EXISTS`, `CAPTCHA_INVALID`, `DOCUMENT_REQUIRES_REVIEW`.

4. Helpers para UI inline

`normalize(...)` ya permite leer el error sin efectos. Podria ser util exponer helpers pequenos para patrones repetidos:

```ts
apiError.message(error, fallback);
apiError.matches(error, { status: 409, errorCode: 'USER_EXISTS' });
```

No es imprescindible, pero evitaria que cada app repita guards utilitarios.
