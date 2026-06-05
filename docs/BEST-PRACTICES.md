# Best Practices

> Tipo: standards

Estandares de codigo para mantener features separadas por capas y evitar mezclar UI,
orquestacion de casos de uso y acceso HTTP.

## Regla principal

El flujo obligatorio dentro de una feature es:

```text
pages/components -> services -> endpoint adapter -> ApiHttpClient -> generated -> API
```

Ninguna `page`, `component` o `store` debe llamar endpoints ni importar
`HttpClient`, `HttpErrorResponse` o `environment.API_URL` directamente.

## Capas por feature

Cada feature vive bajo `src/app/features/<feature>/` y puede usar estas carpetas:

- `pages/`: componentes de ruta. Manejan UI, formularios, navegacion y estado visual.
- `components/`: UI reutilizable dentro de la feature. No contiene llamadas HTTP ni conoce endpoints.
- `services/`: casos de uso y orquestacion. Es la API que consumen pages y components.
- `endpoints/`: **adapters HTTP**. Es la unica capa que importa endpoints
  generados y DTOs de backend. Expone una interfaz publica estable con tipos
  propios de frontend. Si el backend cambia URL, método o shape de respuesta,
  solo este archivo se modifica.
- `store/`: estado local con `signal` y `computed`. No contiene HTTP ni endpoints.
- `models/`: tipos de dominio propios de la feature. No duplicar ahi DTOs que ya
  existan en `src/app/shared/api/generated/models/`.

## Endpoint adapters: contrato estable de API

### Problema que resuelven

Los endpoints generados (`src/app/shared/api/generated/endpoints/`) cambian
cada vez que se ejecuta `npm run update-api`. Si los services importan generated
directo, un rename de URL o de DTO en backend obliga a tocar toda la feature.

### Solucion

Cada feature tiene un archivo `endpoints/<feature>.endpoint.ts` que actua como
**unica puerta de entrada a la API**. Este adapter:

1. Importa endpoints generados y DTOs de backend (capa inestable).
2. Expone metodos publicos con tipos propios de frontend (capa estable).
3. Mapea request/response entre ambos mundos.

### Que es estable (no cambia con update-api)

- Los metodos publicos del adapter (`login()`, `register()`, etc.).
- Los tipos de entrada y salida exportados desde el adapter
  (`LoginPayload`, `LoginResult`, `RegisterPayload`, `RegisterResult`).
- La firma que consumen los services de la feature.

### Que cambia por detras (transparente para el resto de la app)

- La URL real del endpoint (definida en generated).
- El nombre de la constante generada (`postAuthLoginEndpoint`, etc.).
- El shape del DTO de request/response del backend.
- El mapeo interno entre DTO y tipo estable.

### Flujo visual

```text
┌─────────────────────────────────────────────────────────────────────────┐
│  ESTABLE (no cambia con update-api)                                     │
│                                                                         │
│  Page/Component  ──>  Service  ──>  EndpointAdapter.login(payload)      │
│                                         │                               │
│  Tipos:  LoginPayload, LoginResult      │  <── contrato publico         │
└─────────────────────────────────────────│───────────────────────────────┘
                                          │
┌─────────────────────────────────────────│───────────────────────────────┐
│  INESTABLE (cambia con update-api)      ▼                               │
│                                                                         │
│  ApiHttpClient.data(postAuthLoginEndpoint, { body })                    │
│       │                                                                 │
│       ▼                                                                 │
│  POST /Auth/Login  →  AuthRequest  →  DtoAuthenticationResponse         │
│                                                                         │
│  Tipos:  AuthRequest, DtoAuthenticationResponse (generados)             │
└─────────────────────────────────────────────────────────────────────────┘
```

### Reglas

- Solo `endpoints/*.endpoint.ts` importa de `shared/api/generated/`.
- Services, pages, components y stores nunca importan generated directo.
- Si cambia un endpoint en Swagger y se regenera, solo el adapter necesita
  ajuste. El resto de la feature compila sin cambios.
- Cada adapter expone tipos propios simples (no reexporta DTOs del backend).
- Los errores de HTTP se transforman en errores de dominio dentro del adapter.

### Ejemplo: auth

```typescript
// endpoints/auth.endpoint.ts — UNICO archivo que conoce generated

export interface LoginPayload {
  codigoPersona: number;
  password: string;
}
export interface LoginResult {
  documento: string;
}

@Injectable({ providedIn: 'root' })
export class AuthEndpoint {
  login(payload: LoginPayload): Observable<LoginResult> {
    // Internamente usa postAuthLoginEndpoint (generado)
    // Mapea DtoAuthenticationResponse → LoginResult
  }
}
```

```typescript
// services/auth.ts — NO conoce generated, solo el adapter

@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly endpoint = inject(AuthEndpoint);

  login(payload: AuthLoginRequest): Observable<AuthSession> {
    return this.endpoint.login({ ... }).pipe(map(...));
  }
}
```

### Que pasa cuando cambia el backend

| Cambio en backend                         | Impacto en frontend                                         |
| ----------------------------------------- | ----------------------------------------------------------- |
| Rename de URL (`/Auth/Login` → `/v2/...`) | Solo regenerar endpoints. Cero cambios.                     |
| Rename de campo en response               | Ajustar mapper en el adapter. Cero en services.             |
| Nuevo campo obligatorio en request        | Agregar al adapter payload. Ajustar services.               |
| Endpoint eliminado                        | `check-endpoints` detecta. Borrar adapter method + service. |
| Endpoint nuevo                            | Agregar method en adapter con tipos estables.               |

## Tipos generados vs tipos de feature

Para evitar confusion, usar esta regla simple:

- `src/app/shared/api/generated/models/`: contrato tecnico generado desde Swagger.
  Representa DTOs del backend y puede cambiar cuando se regenera con
  `npm run update-models`.
- `src/app/shared/api/generated/endpoints/`: firmas tecnicas de endpoints
  (method, path, request, response) generadas por `npm run update-endpoints`.
- `src/app/features/<feature>/models/`: tipos propios de frontend y dominio de
  la feature. Se usan para formularios, estado local, view models y contratos
  internos entre page/component/service.

Regla de decision:

- Si el tipo describe el request/response real del backend, usar generado.
- Si el tipo describe como la UI trabaja los datos, crear tipo de feature.
- Si el naming del backend no es ideal para UI, mapear en el service y no
  propagar el DTO generado a toda la feature.

Ejemplo en `auth`:

- `AuthRequest` y `DtoAuthenticationResponse` vienen del contrato generado.
- `AuthLoginRequest` y `AuthSession` son tipos de feature para formulario y
  estado de sesion.
- Los services de auth (`AuthSessionService`, `RegistrationService`,
  `PasswordActivationService`) transforman entre ambos modelos.

Cuando una feature crece, se debe dividir por subdominio antes que agregar
archivos genericos como `utils.ts`, `helpers.ts` o `common.ts`.

## Pages vs Components

La distincion clave es la relacion con el router:

- **`pages/`**: el router los instancia directamente. Son dueños del estado de pantalla completa (signals de error, loading, wizard steps, etc.), hacen `inject()` de servicios y coordinan toda la logica del caso de uso.
- **`components/`**: UI reutilizable dentro de la feature, sin relacion con el router. Solo reciben datos via `input()` y emiten eventos via `output()`. No hacen `inject()` de servicios ni tienen estado de negocio.

Ejemplo en `auth`:

```
Register (page)  ──inject──>  RegistrationService  ──inject──>  AuthEndpoint
      │
      └──> AuthForm (component)  ← solo inputs: title, heroIcon, cardSize…
```

`Register` es una page: maneja el estado del formulario multi-paso, llama a
`RegistrationService` y `DocumentPrefillService`, y orquesta la navegacion entre
pasos. `AuthForm` es un component: solo estructura visual que no sabe que datos
va a mostrar ni que hacer con ellos.

## Responsabilidades

- Las pages son dueñas del estado de la pantalla: signals de error, loading, pasos de wizard, navegacion. Hacen `inject()` de servicios y reaccionan a sus respuestas.
- Los components deben quedarse cerca de la presentacion: inputs, outputs, formularios, eventos de usuario, mensajes visibles y bindings. No hacen `inject()` de servicios.
- Los servicios deben coordinar endpoints, stores y transformaciones de dominio. Pueden exponer `Observable`, signals readonly o metodos imperativos segun el caso de uso.
- Los servicios deben ser la capa normal para usar la API. Para endpoints que
  devuelven `OperationResult`, usar `api.data(endpoint, options)` o
  `api.list(endpoint, mapper?, options?)` en vez de repetir `result.data` en cada
  llamada.
- Cada feature debe tener un adapter en `endpoints/` que encapsule los imports
  de generated y DTOs. Los services llaman al adapter, no a `ApiHttpClient`
  directo. Si la feature es trivial (un solo GET), el adapter puede ser inline
  en el service como excepcion documentada.
- Los stores no deben saber de red. Reciben datos ya procesados y exponen estado con `signal`/`computed`.
- Los modelos tecnicos generados viven en `src/app/shared/api/generated/models/`; no se
  editan manualmente. Si se necesitan tipos de dominio propios, ubicarlos dentro
  de la feature y mapearlos desde/hacia el contrato generado.

## Codegen de API

Cuando cambia el contrato publicado por Swagger:

```bash
npm run update-api
```

Esto ejecuta:

- `npm run update-models`: regenera modelos en `src/app/shared/api/generated/models/`.
- `npm run update-endpoints`: regenera constantes en
  `src/app/shared/api/generated/endpoints/`.

Para detectar drift en CI o antes de un PR:

```bash
npm run check-endpoints
```

Si el generador muestra warnings por schemas ambiguos, la correccion debe hacerse
en el backend Swagger. El frontend no debe corregir manualmente rutas, request
bodies ni response types generados.

## Angular 21 y zoneless

El proyecto usa Angular standalone, `OnPush` por defecto y
`provideZonelessChangeDetection()`. Para mantener compatibilidad:

- preferir `inject()` sobre constructor injection;
- usar `signal` y `computed` para estado leido por templates;
- evitar mutaciones asincronas invisibles para change detection;
- usar `async` pipe, `toSignal` o limpieza explicita cuando una suscripcion viva mas que un request HTTP autocompletado;
- no introducir `httpResource` para mutaciones imperativas como login, registro o submits `POST`; usar servicios y `Observable`.

## Accesibilidad de UI

Toda pantalla o componente nuevo debe cumplir WCAG 2.2 AA y seguir
[docs/ACCESSIBILITY.md](./ACCESSIBILITY.md).

Reglas practicas:

- usar componentes ORT existentes antes que controles custom;
- usar landmarks, headings, labels visibles, hints y errores por campo;
- agregar `OrtErrorSummary` en formularios con mas de un campo validable;
- habilitar links del resumen al campo solo si el componente expone un target
  publico estable; en controles ORT actuales usar
  `ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED`;
- asegurar navegacion por teclado, foco visible y soporte de `Escape` en menus o dialogos;
- reflejar informacion visual decorativa en texto accesible cuando corresponda,
  por ejemplo prefijos telefonicos;
- no parchear `@desarrolloort/components` desde la app.

Si falta soporte accesible en un componente ORT, dejar
`TODO(a11y-ort-component): ...` en el punto de uso y registrar el gap en
[docs/ACCESSIBILITY.md](./ACCESSIBILITY.md). No usar overrides contra DOM o
clases internas de ORT.

## Testing

Los tests `.spec.ts` deben estar co-localizados junto al archivo fuente. El
directorio legacy `tests/` sigue siendo aceptado temporalmente por los scripts,
pero la ubicacion preferida es junto al source.

- Los endpoints se prueban con `HttpTestingController`.
- Los servicios se prueban mockeando endpoints.
- Las pages y components se prueban mockeando servicios, no endpoints.
- Los stores se prueban como estado puro, sin HTTP.
- Los flujos criticos de UI deben tener cobertura Playwright `@smoke` o
  `@regression` segun su criticidad. Ver
  [docs/E2E-GUARDRAILS.md](./E2E-GUARDRAILS.md).
- Para features grandes nuevas como becas o inscripciones, usar
  acceptance-first: escribir los escenarios Playwright desde Figma y criterios
  funcionales antes de implementar.
- Los datos sensibles de E2E viven en `.env.e2e.local` o secrets de GitHub; no
  se versionan en specs, fixtures ni docs.

## Enforcement

`eslint.config.js` bloquea imports prohibidos en features:

- `@angular/common/http` y `src/environments/environment` dentro de features;
- imports de endpoints generados o manuales desde `pages/`, `components/` y `store/`.

Si una excepcion parece necesaria, primero revisar si corresponde crear o ampliar
un service de feature.

## Referencias oficiales

- [Angular coding style guide](https://angular.dev/style-guide)
- [Angular HTTP best practices](https://angular.dev/guide/http/making-requests)
- [Angular zoneless guide](https://angular.dev/guide/zoneless)
- [Angular signals guide](https://angular.dev/guide/signals)
