---
slug: /arquitectura
title: Arquitectura
description: Capas, responsabilidades y flujo de datos del frontend de Admisiones.
---

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
pages/components -> services -> endpoint adapter -> ApiHttpClient -> generated -> API
```

Capas esperadas:

- `pages/`: componentes de ruta, formularios, navegacion y estado visual.
- `components/`: UI reutilizable de la feature.
- `services/`: casos de uso y orquestacion consumidos por pages/components.
- `endpoints/`: adaptadores HTTP obligatorios para llamadas a API. Es la unica
  capa de una feature que importa contratos generados y usa `ApiHttpClient`.
- `store/`: estado local con `signal` y `computed`.
- `models/`: contratos, DTOs y errores de dominio.

Las pages, components, stores, facades y services no deben importar contratos
generados ni usar `HttpClient` directamente. Ver
[docs/BEST-PRACTICES.md](./BEST-PRACTICES.md).

## Contratos API generados

El backend mantiene la fuente de verdad del contrato HTTP en Swagger. El
frontend genera localmente dos salidas tecnicas ignoradas por Git:

- `src/app/shared/api/generated/models/`: modelos TypeScript generados por
  `npm run update-api`.
- `src/app/shared/api/generated/endpoints/`: constantes de endpoint generadas
  por `npm run update-api`.

El comando recomendado para actualizar ambos contratos es:

```bash
npm run update-api
```

Los endpoints generados solo describen `operationId`, metodo, path, parametros,
request y response. No contienen logica funcional ni reemplazan los servicios de
feature. La ejecucion centralizada vive en
`src/app/shared/api/core/api-http-client.ts`, que usa `environment.API_URL`,
`HttpClient` y `buildApiPath`. Para los `OperationResult` del backend, los
adapters deben usar la receta unica: `api.data(...)` para un item, `api.list(...)`
para arrays, `requestWithMessage(...)` solo si necesitan `message`, y `void`
cuando el POST no devuelve data util. Los mappers viven en el adapter y no leen
`.data` a mano en cada llamada.

Acoplamiento esperado:

```text
Swagger backend
  -> npm run update-api
  -> modelos y endpoints generados
  -> endpoint adapters de feature
  -> servicios de aplicacion
  -> pages, components, stores
```

Reglas:

- no editar manualmente archivos generados;
- no importar endpoints generados fuera de `features/*/endpoints/*.endpoint.ts`;
- mantener nombres funcionales, mapeos de UI y orquestacion dentro de la feature;
- usar `npm run check-api-contracts` para validar que los adapters no filtren
  `generated`, `unknown`, `any` ni casts `as unknown as`.
- usar `npm run update-api` para regenerar endpoints y validar adapters.

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

`src/environments/environment.ts` queda versionado como wrapper estable. Los archivos generados `src/environments/generated-environment.ts` y `src/web.config` no se versionan: en local los crea `npm run env:sync` desde Azure App Configuration y los workflows de CI los generan antes de compilar.

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
  endpoints/catalogs.endpoint.ts   ← generated, ApiHttpClient y mapeos HTTP
  services/catalogs.ts             ← API pública para otras features
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

- [README.md](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/README.md)
- [docs/SETUP.md](./SETUP.md)
- [docs/WORKFLOW.md](./WORKFLOW.md)
- [docs/BEST-PRACTICES.md](./BEST-PRACTICES.md)
