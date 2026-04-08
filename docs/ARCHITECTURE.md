# Architecture

> Tipo: explanation

Resumen de las decisiones estructurales de la plantilla para que un proyecto nuevo pueda usarla como base y luego adaptarla sin romper sus fundamentos.

## Tipo de repositorio

Template frontend base con Angular 20, Angular Material, Jest y GitHub Actions para CI/CD.

## Estructura principal

- `src/main.ts`: punto de entrada de la aplicacion.
- `src/app/app.config.ts`: configuracion global de proveedores.
- `src/app/app.routes.ts`: definicion central de rutas.
- `src/app/components/`: componentes reutilizables de alto nivel.
- `src/app/services/`: servicios de logica y manejo transversal.
- `src/app/interceptors/`: interceptores HTTP.
- `src/app/utils/`: constantes y utilidades compartidas.
- `src/environments/`: configuracion por ambiente.
- `tests/`: replica la estructura de `src/app/` para tests unitarios.
- `scripts/testing/`: scripts para detectar o generar tests faltantes.
- `.github/workflows/`: automatizacion de CI, despliegues, releases y etiquetado.

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

### Tests fuera de `src/`

Los tests viven en `tests/` y no junto al archivo fuente. La regla del repositorio es mantener la misma estructura relativa respecto a `src/app/`.

### Ambientes locales y generados

Los archivos de `src/environments/` no se versionan. En local se crean a partir de los templates y en CI se generan mediante [`.github/actions/setup-env/action.yml`](../.github/actions/setup-env/action.yml).

## Puntos de extension esperados

- Agregar rutas reales en `src/app/app.routes.ts`.
- Reemplazar el contenido de ejemplo de `AppComponent`.
- Incorporar nuevos modulos de dominio bajo `src/app/`.
- Ajustar dependencias, workflows, contratos y configuracion de despliegue segun el proyecto que use la plantilla.

## Limites de esta base

- No todas las dependencias incluidas son obligatorias para todos los proyectos derivados.
- No todos los workflows incluidos deben mantenerse tal cual en todos los repositorios nuevos.
- La arquitectura final del proyecto no tiene por que coincidir exactamente con la de esta plantilla.

## Referencias relacionadas

- [README.md](../README.md)
- [docs/SETUP.md](./SETUP.md)
- [docs/WORKFLOW.md](./WORKFLOW.md)
- [docs/BEST-PRACTICES.md](./BEST-PRACTICES.md)
