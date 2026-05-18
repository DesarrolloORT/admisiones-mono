# Architecture

> Tipo: explanation

Resumen de las decisiones estructurales vigentes del proyecto.

## Tipo de repositorio

Frontend Angular 21 standalone con Angular Material, Vitest y GitHub Actions para CI/CD.

## Estructura principal

- `src/main.ts`: punto de entrada de la aplicacion.
- `src/app/app.config.ts`: configuracion global de proveedores.
- `src/app/app.routes.ts`: definicion central de rutas.
- `src/app/core/`: servicios, interceptores y constantes transversales.
- `src/app/features/`: funcionalidades de producto separadas por dominio.
- `src/app/shared/`: UI, utilidades reutilizables, modelos API generados y core HTTP.
- `src/environments/`: configuracion por ambiente.
- `scripts/codegen/`: generacion de contratos desde Swagger.
- `scripts/testing/`: scripts para detectar o generar tests faltantes.
- `.github/workflows/`: automatizacion de CI, despliegues, releases y etiquetado.

## Estructura por feature

Cada feature bajo `src/app/features/<feature>/` debe respetar el flujo:

> **Nota:** No todas las features representan dominios de negocio. Algunas, como
> `catalogs/`, agrupan datos transversales consumidos por múltiples features.
> Ver [Features transversales](#features-transversales).

```text
pages/components -> services -> ApiHttpClient -> endpoints generados -> API
```

Capas esperadas:

- `pages/`: componentes de ruta, formularios, navegacion y estado visual.
- `components/`: UI reutilizable de la feature.
- `services/`: casos de uso y orquestacion consumidos por pages/components.
- `endpoints/`: adaptadores HTTP opcionales para casos especiales. En flujos
  simples, el service de feature consume `ApiHttpClient` y endpoints generados
  directamente.
- `store/`: estado local con `signal` y `computed`.
- `models/`: contratos, DTOs y errores de dominio.

Las pages, components y stores no deben importar endpoints ni `HttpClient`
directamente. Ver [docs/BEST-PRACTICES.md](./BEST-PRACTICES.md).

## Contratos API generados

El backend mantiene la fuente de verdad del contrato HTTP en Swagger. El
frontend versiona dos salidas generadas:

- `src/app/shared/api-models/`: modelos TypeScript generados por
  `npm run update-models`.
- `src/app/shared/api/endpoints/generated/`: constantes de endpoint generadas
  por `npm run update-endpoints`.

El comando recomendado para actualizar ambos contratos es:

```bash
npm run update-api
```

Los endpoints generados solo describen `operationId`, metodo, path, parametros,
request y response. No contienen logica funcional ni reemplazan los servicios de
feature. La ejecucion centralizada vive en
`src/app/shared/api/core/api-http-client.service.ts`, que usa
`environment.API_URL`, `HttpClient` y `buildApiPath`. Para los `OperationResult`
del backend, los servicios deben preferir `api.data(...)` o `api.list(...)` y no
leer `.data` a mano en cada llamada.

Acoplamiento esperado:

```text
Swagger backend
  -> npm run update-api
  -> modelos y endpoints generados
  -> servicios de aplicacion
  -> pages, components, stores
```

Reglas:

- no editar manualmente archivos generados;
- no importar endpoints generados desde pages, components o stores;
- mantener nombres funcionales, mapeos de UI y orquestacion dentro de la feature;
- usar `npm run check-api-contracts` cuando se quiera validar drift contra Swagger.

`ApiHttpClient` cachea por defecto los `GET` sin `pathParams` ni
`queryParams`. Esto cubre catálogos y datos de referencia sin agregar
`shareReplay` en cada service. Los `GET` con parámetros no se cachean
automáticamente porque normalmente dependen del filtro recibido. Para invalidar
el cache compartido, llamar a `api.clearCache()` desde el service que corresponda.

## Decisiones tecnicas vigentes

### Standalone Angular

La app esta montada con configuracion standalone. La composicion principal vive en `src/main.ts`, `src/app/app.config.ts` y `src/app/app.routes.ts`, sin `AppModule`.

### Change detection zoneless

`src/app/app.config.ts` usa `provideZonelessChangeDetection()`. Cualquier extension del template debe ser compatible con ese modelo de deteccion de cambios.

### Configuracion global centralizada

`src/app/app.config.ts` concentra:

- router con `withComponentInputBinding()` y `withInMemoryScrolling()`;
- cliente HTTP con `withInterceptors([httpInterceptor])`;
- locale `es-UY`;
- configuracion de Angular Material y Moment para fechas;
- inicializacion de iconografia mediante `UiUtils.initializeMaterialSymbols()`.

### Tests co-localizados

Los tests `.spec.ts` viven junto al archivo fuente. Los scripts todavia aceptan
el directorio legacy `tests/` para no romper migraciones existentes, pero la
ubicacion preferida es co-localizada.

### Ambientes locales y generados

Los archivos de `src/environments/` no se versionan. En local se crean a partir de los templates y en CI se generan mediante [`.github/actions/setup-env/action.yml`](../.github/actions/setup-env/action.yml).

## Puntos de extension esperados

- Agregar rutas reales en `src/app/app.routes.ts`.
- Reemplazar el contenido de ejemplo de `AppComponent`.
- Incorporar nuevas features bajo `src/app/features/` respetando capas.
- Ajustar dependencias, workflows, contratos y configuracion de despliegue segun el proyecto que use la plantilla.

## Limites de esta base

- No todas las dependencias incluidas son obligatorias para todos los proyectos derivados.
- No todos los workflows incluidos deben mantenerse tal cual en todos los repositorios nuevos.
- La arquitectura final del proyecto no tiene por que coincidir exactamente con la de esta plantilla.

## Features transversales

Algunas carpetas bajo `src/app/features/` no son features de negocio sino
módulos transversales que exponen datos o utilidades a múltiples features.

### `catalogs/`

Agrupa endpoints de datos de referencia (países, bachilleratos, instituciones, etc.).
Cualquier feature puede inyectar `Catalogs` (service) para obtener listas de
catálogos. El cache lo aplica `ApiHttpClient` automáticamente porque estos
endpoints son `GET` sin parámetros.

Estructura:

```text
features/catalogs/
  services/catalogs.ts             ← ApiHttpClient, endpoints generados, mapeos y API pública
  models/catalog.interface.ts      ← tipos de cada catálogo
```

Uso desde otra feature:

```ts
private catalogs = inject(Catalogs);

this.catalogs.getCountries().subscribe(countries => ...);
```

Si necesitás invalidar el cache compartido (por ejemplo después de un cambio de
sesión), llamá a `catalogs.clearCache()`.

## Referencias relacionadas

- [README.md](../README.md)
- [docs/SETUP.md](./SETUP.md)
- [docs/WORKFLOW.md](./WORKFLOW.md)
- [docs/BEST-PRACTICES.md](./BEST-PRACTICES.md)
