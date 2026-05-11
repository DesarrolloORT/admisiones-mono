# Best Practices

> Tipo: standards

Estandares de codigo para mantener features separadas por capas y evitar mezclar UI,
orquestacion de casos de uso y acceso HTTP.

## Regla principal

El flujo obligatorio dentro de una feature es:

```text
pages/components -> services -> endpoints -> HttpClient/API
```

Ninguna `page`, `component` o `store` debe llamar endpoints ni importar
`HttpClient`, `HttpErrorResponse` o `environment.API_URL` directamente.

## Capas por feature

Cada feature vive bajo `src/app/features/<feature>/` y puede usar estas carpetas:

- `pages/`: componentes de ruta. Manejan UI, formularios, navegacion y estado visual.
- `components/`: UI reutilizable dentro de la feature. No contiene llamadas HTTP ni conoce endpoints.
- `services/`: casos de uso y orquestacion. Es la API que consumen pages y components.
- `endpoints/`: unica capa de la feature que puede importar `HttpClient`, `HttpErrorResponse`, `environment.API_URL` y construir URLs.
- `store/`: estado local con `signal` y `computed`. No contiene HTTP ni endpoints.
- `models/`: DTOs y contratos generados automaticamente desde la API del backend via `swagger_gen`. No se deben agregar ni modificar archivos en esta carpeta manualmente; el contenido es sobreescrito en cada regeneracion.

Cuando una feature crece, se debe dividir por subdominio antes que agregar
archivos genericos como `utils.ts`, `helpers.ts` o `common.ts`.

## Responsabilidades

- Los componentes deben quedarse cerca de la presentacion: inputs, outputs, formularios, eventos de usuario, mensajes visibles y bindings.
- Los servicios deben coordinar endpoints, stores y transformaciones de dominio. Pueden exponer `Observable`, signals readonly o metodos imperativos segun el caso de uso.
- Los endpoints deben ser pequenos, testeables y sin estado de UI. Solo arman request, URL, opciones HTTP y traducen errores HTTP a errores de dominio.
- Los stores no deben saber de red. Reciben datos ya procesados y exponen estado con `signal`/`computed`.
- Los modelos son autogenerados; no introducir tipos manuales en `models/`. Si se necesitan tipos de dominio propios, ubicarlos en `services/` o en un archivo dedicado fuera de `models/`.

## Angular 21 y zoneless

El proyecto usa Angular standalone, `OnPush` por defecto y
`provideZonelessChangeDetection()`. Para mantener compatibilidad:

- preferir `inject()` sobre constructor injection;
- usar `signal` y `computed` para estado leido por templates;
- evitar mutaciones asincronas invisibles para change detection;
- usar `async` pipe, `toSignal` o limpieza explicita cuando una suscripcion viva mas que un request HTTP autocompletado;
- no introducir `httpResource` para mutaciones imperativas como login, registro o submits `POST`; usar servicios y `Observable`.

## Testing

Los tests `.spec.ts` deben estar co-localizados junto al archivo fuente. El
directorio legacy `tests/` sigue siendo aceptado temporalmente por los scripts,
pero la ubicacion preferida es junto al source.

- Los endpoints se prueban con `HttpTestingController`.
- Los servicios se prueban mockeando endpoints.
- Las pages y components se prueban mockeando servicios, no endpoints.
- Los stores se prueban como estado puro, sin HTTP.

## Enforcement

`eslint.config.js` bloquea imports prohibidos en features:

- `@angular/common/http` y `src/environments/environment` fuera de `features/**/endpoints/`;
- imports de `endpoints/` desde `pages/`, `components/` y `store/`.

Si una excepcion parece necesaria, primero revisar si corresponde crear o ampliar
un service de feature.

## Referencias oficiales

- [Angular coding style guide](https://angular.dev/style-guide)
- [Angular HTTP best practices](https://angular.dev/guide/http/making-requests)
- [Angular zoneless guide](https://angular.dev/guide/zoneless)
- [Angular signals guide](https://angular.dev/guide/signals)

