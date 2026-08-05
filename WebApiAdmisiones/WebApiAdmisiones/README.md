# WebApiAdmisiones (host)

**El composition root.** Es lo único que conoce a todos los módulos y lo único que sabe de HTTP.

## Qué resuelve

Traducir HTTP ↔ dominio y armar el grafo de dependencias. **No tiene lógica de negocio**: los
controllers inyectan casos de uso y devuelven lo que éstos responden.

## Los 6 controllers

| Controller | Ruta base | Módulo que consume |
|---|---|---|
| `AuthController` | `auth` | Authentication |
| `RegistrationController` | `registration` | Registration (+ `Core/AzureService`) |
| `PersonController` | `person` | People |
| `EnrollmentsController` | `enrollments` | Enrollments |
| `CatalogsController` | `catalogs` | Catalogs |
| `ScholarshipsController` | `scholarships` | Scholarships |

Son 39 endpoints. Las rutas son **explícitas y en kebab-case** (`[Route("auth")]`,
`[HttpPost("verify-two-factor-code")]`), no `[Route("[controller]")]`: así el nombre de la clase deja
de ser parte del contrato HTTP y se puede renombrar sin romper nada.

`ApiBaseController<T>` da `ValidateResponse(OperationResult)`, que traduce el `HttpCode` del
`OperationResult` al status code de la respuesta.

## DI: `Extensions/DomainServicesExtensions.cs`

Cada módulo registra lo suyo con su propia extensión. El host solo las llama:

```csharp
services.AddIdentityModule();
services.AddTivenosIntegration();
services.AddPeople();
services.AddAuthenticationModule();
services.AddScholarships();
services.AddRegistration();
services.AddEnrollments();
```

Lo que queda acá es lo que **solo el host puede decidir**:

- El decorador de cache de catálogos (`CatalogCacheDecorator`), que envuelve a `ICatalogService`.
- El `HttpClient` de la API de Inscripciones y Pagos (`AddEnrollmentsAndPaymentsApiClient`).
- Servicios propios de HTTP: `ICurrentUserService`, captcha, rate limiting de ASP.NET.

## `Security/` — todo lo que pasa antes del controller

| Carpeta | Qué hay |
|---|---|
| `Authentication/` | `ICurrentUserService`, cookies de sesión y de activación |
| `Cache/` | `CatalogCacheDecorator` — cachea 3 catálogos en Redis, TTL `Cache:CatalogosTTLHours` |
| `Captcha/` | `RequireCaptchaAttribute` + `CaptchaActions` |
| `Middleware/` | logging de model binding y de errores |
| `Observability/` | `LoggingHelper`, telemetría de cliente |
| `RateLimiting/` | políticas de ASP.NET (`EnableRateLimiting("ReconocimientoDocumento")`) |
| `RequestValidation/` | `JsonSchemaValidationFilter` |

### ⚠️ `CaptchaActions`: los valores son contrato

```csharp
public const string EvaluateDocument = "EvaluarDocumento";
```

El **identificador** está en inglés, el **literal** sigue en español a propósito: viaja a reCAPTCHA y
tiene que coincidir con la action que dispara el front. Renombrar el string rompe la validación de
captcha en producción.

## `Models/UploadRequests.cs`

Los DTOs de subida de archivos (`FilePayload`, `UploadPersonPhotoRequest`, …) viven acá y no en un
módulo porque son **forma de transporte HTTP**, no dominio. El controller los traduce a los DTOs del
módulo (`IdentityDocumentFile`, etc.).

## `Docs/contracts/*.json`

Tres contratos que **lee el front** y que verifican tests del backend:

| Archivo | Test |
|---|---|
| `carreras.contract.json` | `CarrerasContractTests` |
| `inscripcion-detalle.contract.json` | `InscripcionDetalleContractTests` |
| `encuesta-inicial.contract.json` | `EncuestaInicialContractTests` |

Si cambiás un DTO que aparece en alguno, **actualizá el JSON en el mismo cambio** o el test se pone
rojo. Son contenido copiado al output: si un test falla con `NullReferenceException` leyéndolos,
probablemente estés corriendo con `--no-build` sobre un build viejo.

## Variables de entorno

`OracleConnectionStringAdmisiones`, `RedisConnectionStringAdmisiones`, `JWT_SECRET_KEY`,
`JWT_ISSUER_TOKEN_ADMISIONES`, `JWT_AUDIENCE_TOKEN_ADMISIONES`, `JWT_EXPIRE_MINUTES_ADMISIONES`,
`JWT_REFRESH_EXPIRE_ADMISIONES`, `PASSWORD_ACTIVATION_SECRET_KEY`, `SECRET_KEY_API_INSCR_PAGOS`,
`RECAPTCHA_SECRET_KEY`, `AUTH_COOKIE_DOMAIN` (opcional).
Ver `Extensions/ENVIRONMENT_CONFIGURATION.md`.

### Secretos que salieron de `appsettings`

`ApiServicioInterno` (usuario y contraseña del servicio interno que consume `Core/LdapService`)
estaba **hardcodeado en los 6 `appsettings`, incluido Production**, y versionado en git. Ahora va por
variable de entorno con `__` como separador de nivel:

```
ApiServicioInterno__RutaApi
ApiServicioInterno__Usuario
ApiServicioInterno__Password
```

No hizo falta tocar `Core`: `WebApplication.CreateBuilder` ya incluye `AddEnvironmentVariables()`, y
`ApiServicioInterno__Password` sobrescribe la clave `ApiServicioInterno:Password` que `Core` lee.

`Extensions/RequiredConfigurationExtensions.cs` corta el arranque si falta alguna. Es necesario
porque `Core/LdapService` resuelve con `?? string.Empty`: sin la validación la app levantaría igual y
mandaría credenciales vacías, y el síntoma aparecería recién como un fallo de autenticación contra el
servicio interno. Si agregás otro secreto obligatorio, sumalo a `RequiredKeys`.

## Trampas

- **Ninguna configuración está acoplada a paths.** Rate limiting, cache y captcha se declaran por
  atributo sobre la acción, no por ruta. Por eso renombrar rutas es seguro del lado backend.
- Los `ProducesResponseType` son solo metadata de Swagger: **no** los verifica el compilador. Si
  cambiás el tipo que devuelve un servicio, el atributo puede quedar mintiendo sin que nada falle.
  Ya pasó.
- `loadtest/get-endpoints.k6.js` (fuera de esta carpeta) tiene la lista de todos los `[HttpGet]`. El
  `CLAUDE.md` del repo obliga a actualizarla en el mismo cambio que toque una ruta o sus params.
- Los `Docs/*.md` de esta carpeta son documentación histórica de features (rate limiting, service
  token, flujos). Tienen las rutas actualizadas, pero parte de la prosa describe nombres de clases
  anteriores al refactor.

## Dependencias vulnerables: `Directory.Build.props`

`WebApiAdmisiones/Directory.Build.props` fija versiones parcheadas de tres paquetes que llegan como
**transitivos desde el submódulo `Core`** (`Core/MailORT` y `Core/LdapService` los declaran en
versiones con CVEs):

| Paquete | Core declara | Se fija en | Advisories que cierra |
|---|---|---|---|
| `CoreWCF.Primitives` | 1.9.0 | 1.9.1 | 1 crítica, 4 altas, 2 moderadas, 1 baja |
| `CoreWCF.NetFramingBase` | 1.9.0 | 1.9.1 | 1 alta |
| `System.Security.Cryptography.Xml` | 10.0.7 | 10.0.10 | 5 altas |

**No es el arreglo de fondo.** Lo correcto es subir las versiones en `Core`, que es un repo aparte
compartido con otros proyectos ORT. Mientras eso no pase, esto evita que se despliegue el binario
vulnerable desde acá.

Cuando `Core` se actualice: borrar `Directory.Build.props` y verificar que sigan en cero con
`dotnet build WebApiAdmisiones.sln -v q 2>&1 | grep -c NU190`.

Los ~76 warnings `NU190x` que quedan son de `MailORT` y `LdapService` auditando **su propio** grafo;
no afectan lo que compila este repo.

## Documentación de los módulos

Cada proyecto de `AppLogic.*` tiene su propio `README.md`. Ver
[docs/README-MODULOS.md](../../docs/README-MODULOS.md) para el índice y el mapa de dependencias.
