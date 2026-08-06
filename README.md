# API Admisiones

API REST de admisiones de ORT Uruguay sobre `.NET 10`. Expone los endpoints de autenticación,
onboarding, catálogos, persona, inscripciones y becas que consume el front de Admisiones.

## Documentación

**Antes de tocar código de un módulo, leé su `README.md`** (está dentro del propio proyecto).

| Documento | Para qué |
|---|---|
| [docs/README-MODULOS.md](docs/README-MODULOS.md) | Índice de los 12 módulos, niveles de dependencia y reglas que no se rompen |
| [docs/GLOSARIO-DOMINIO.md](docs/GLOSARIO-DOMINIO.md) | Español→inglés del dominio; qué se traduce y qué no |
| [docs/MATRIZ-TRAZABILIDAD.md](docs/MATRIZ-TRAZABILIDAD.md) | Por qué las cosas están como están; breaking changes con el front |
| [docs/GUIA-ESTILO-CODIGO.md](docs/GUIA-ESTILO-CODIGO.md) | Cómo escribir código nuevo acá |
| [WebApiAdmisiones/WebApiAdmisiones/README.md](WebApiAdmisiones/WebApiAdmisiones/README.md) | El host: controllers, DI, seguridad HTTP |

READMEs por módulo:

| Módulo | Nivel | Qué resuelve |
|---|---|---|
| [AppLogic.Contracts](WebApiAdmisiones/AppLogic/AppLogic.Contracts/README.md) | 1 | lo poco que comparten todos los módulos |
| [AppLogic.DevartDtos](WebApiAdmisiones/AppLogic/AppLogic.DevartDtos/README.md) | 1 | **código generado, no se edita** |
| [AppLogic.Platform](WebApiAdmisiones/AppLogic/AppLogic.Platform/README.md) | 2 | mail, rate limiting, serialización |
| [AppLogic.Integrations.Tivenos](WebApiAdmisiones/AppLogic/AppLogic.Integrations.Tivenos/README.md) | 2 | encola avisos al CRM |
| [AppLogic.Integrations.EnrollmentsAndPayments](WebApiAdmisiones/AppLogic/AppLogic.Integrations.EnrollmentsAndPayments/README.md) | 2 | cliente de la API interna de Inscripciones y Pagos |
| [AppLogic.Identity](WebApiAdmisiones/AppLogic/AppLogic.Identity/README.md) | 3 | documento de identidad y stores de Redis |
| [AppLogic.People](WebApiAdmisiones/AppLogic/AppLogic.People/README.md) | 4 | datos de la persona autenticada |
| [AppLogic.Authentication](WebApiAdmisiones/AppLogic/AppLogic.Authentication/README.md) | 4 | login, 2FA, tokens, contraseña inicial |
| [AppLogic.Scholarships](WebApiAdmisiones/AppLogic/AppLogic.Scholarships/README.md) | 4 | becas |
| [AppLogic.Registration](WebApiAdmisiones/AppLogic/AppLogic.Registration/README.md) | 5 | onboarding |
| [AppLogic.Enrollments](WebApiAdmisiones/AppLogic/AppLogic.Enrollments/README.md) | 5 | interés, preinscripción, pagos, encuesta inicial |
| [AppLogic.Catalogs](WebApiAdmisiones/AppLogic/AppLogic.Catalogs/README.md) | 6 | combos de solo lectura |

## Estructura

```text
api-admisiones/
├── Core/                      submódulo compartido con otros proyectos ORT (NO se modifica)
│   ├── AzureService/          reconocimiento de documentos (Azure Document Intelligence)
│   ├── DbConnectionContext/   secuencias e IDs de Oracle
│   ├── LdapService/           usuarios y contraseñas
│   ├── MailORT/               envío de mail (cliente SOAP Office365)
│   ├── Modules/               ModBandeja, ModGenericBase
│   └── Utilities/             OperationResult<T>, Constantes, Encriptador
├── WebApiAdmisiones/
│   ├── WebApiAdmisiones/      host: Program.cs, Controllers/, Extensions/, Security/
│   ├── AppLogic.*/            12 módulos por área (ver tabla de arriba)
│   ├── BusinessLogic/         entidades e interfaces de repositorio (Devart, generado)
│   ├── DataAccess/            contextos y repositorios Devart/Oracle (generado)
│   ├── UnitTesting/           xUnit + Moq + cobertura
│   └── Directory.Build.props  pins de seguridad de dependencias transitivas de Core
├── docs/                      glosario, matriz, guía de estilo, mapa de módulos
├── loadtest/                  k6: tiempos de respuesta de todos los GET
└── .github/workflows/         CI, SonarQube, imagen Docker
```

Los módulos respetan niveles: uno solo puede referenciar módulos de nivel **estrictamente menor**.
No hay ciclos.

## Stack

`.NET 10` · `ASP.NET Core Web API` · `EF Core` con proveedor **Devart para Oracle** · **Redis**
(rate limiting, sesiones 2FA, flujo de registro, cache de catálogos) · **LDAP** (contraseñas) ·
`JWT Bearer` + cookies HttpOnly · Swagger · Serilog + OpenTelemetry + prometheus-net ·
xUnit + Moq + coverlet.

## Endpoints

39 endpoints en 6 controllers. Las rutas son **inglés en kebab-case**.

| Controller | Ruta base | Endpoints |
|---|---|---|
| `AuthController` | `auth` | `login`, `logout`, `refresh-token`, `recover-password`, `verify-two-factor-code`, `resend-two-factor-code`, `activate-password-link`, `complete-initial-password` |
| `RegistrationController` | `registration` | `evaluate-document`, `verify-identity`, `analyze-attachment`, `confirm-new-person`, `confirm-registration-request` |
| `PersonController` | `person` | `details` (GET/PUT), `photo` (GET/POST), `identity-document` (GET/POST), `enrollments`, `scholarships`, `change-password`, `validate-phone-number` |
| `EnrollmentsController` | `enrollments` | `product-interest`, `initial-survey` (GET/POST), `confirm-pre-enrollment`, `reactivate`, `start-payment`, `student-regulations`, `details` |
| `CatalogsController` | `catalogs` | `countries-states-cities`, `initial-survey`, `degree-programs`, `intakes`, `shifts`, `banks`, `institutions` |
| `ScholarshipsController` | `scholarships` | `enrollments` |

Los payloads viajan **en inglés**. El detalle de cada contrato está en
`WebApiAdmisiones/WebApiAdmisiones/Docs/contracts/*.json`, que además verifican tests del backend.

## Requisitos

- `.NET SDK 10`
- acceso al repo **con submódulos** (`Core/` es obligatorio para compilar)
- Oracle del ambiente que se quiera usar
- Redis
- las variables de entorno de abajo

## Clonado

```bash
git clone --recurse-submodules git@github.com:DesarrolloORT/api-admisiones.git
cd api-admisiones
```

Si ya lo clonaste sin submódulos:

```bash
git submodule update --init --recursive
```

## Variables de entorno

Sin estas la API no levanta:

```powershell
$env:OracleConnectionStringAdmisiones="Data Source=...;User Id=...;Password=..."
$env:RedisConnectionStringAdmisiones="localhost:6379"

$env:JWT_SECRET_KEY="..."
$env:JWT_ISSUER_TOKEN_ADMISIONES="https://webapiadmisiones.ort.edu.uy"
$env:JWT_AUDIENCE_TOKEN_ADMISIONES="https://webapiadmisiones.ort.edu.uy"
$env:JWT_EXPIRE_MINUTES_ADMISIONES="60"
$env:JWT_REFRESH_EXPIRE_ADMISIONES="1440"

$env:PASSWORD_ACTIVATION_SECRET_KEY="..."   # firma del link de activación de contraseña
$env:SECRET_KEY_API_INSCR_PAGOS="..."       # token servicio-a-servicio hacia Inscripciones y Pagos
$env:RECAPTCHA_SECRET_KEY="..."             # validación de captcha en los endpoints públicos

# Servicio interno usado por LDAP. Antes estaban en appsettings; se sacaron por ser credenciales.
$env:ApiServicioInterno__RutaApi="https://WebApiServicioInterno.ort.edu.uy/"
$env:ApiServicioInterno__Usuario="..."
$env:ApiServicioInterno__Password="..."
```

Las tres últimas usan `__` como separador de nivel: así ASP.NET Core las mapea a
`ApiServicioInterno:Usuario` sin necesidad de tocar código. **Si falta alguna la app no levanta**
(`RequiredConfigurationExtensions.ValidateRequiredSecrets`, se ejecuta antes que nada en
`Program.cs`); el consumidor vive en `Core/LdapService` y resuelve con `?? string.Empty`, así que sin
esa validación mandaría credenciales vacías y el error aparecería recién como fallo de autenticación.

Opcional: `AUTH_COOKIE_DOMAIN` (dominio de las cookies de sesión), `ASPNETCORE_ENVIRONMENT`.

El resto de la configuración va por ambiente en `appsettings.{Development,LocalHost,Testing,
Preproduction,Production}.json`. La lógica de ambientes está en
[ENVIRONMENT_CONFIGURATION.md](WebApiAdmisiones/WebApiAdmisiones/Extensions/ENVIRONMENT_CONFIGURATION.md).

## Build y ejecución

```bash
dotnet restore WebApiAdmisiones/WebApiAdmisiones.sln
dotnet build WebApiAdmisiones/WebApiAdmisiones.sln -c Debug

dotnet run --project WebApiAdmisiones/WebApiAdmisiones/WebApiAdmisiones.csproj --launch-profile http
```

- `http://localhost:8080/swagger`
- `https://localhost:7150/swagger`

Swagger queda habilitado solo en `Development` y `Testing`.

## Tests

```bash
dotnet test WebApiAdmisiones/UnitTesting/UnitTesting.csproj

dotnet test WebApiAdmisiones/UnitTesting/UnitTesting.csproj \
  --settings WebApiAdmisiones/UnitTesting/tests.runsettings --collect:"XPlat Code Coverage"
```

**1036 tests**, todos verdes. Los reportes (`cobertura` y `opencover`) quedan en
`WebApiAdmisiones/UnitTesting/TestResults/`.

Load test de todos los `GET`:

```bash
k6 run loadtest/get-endpoints.k6.js
```

## CI

- `.github/workflows/ci-sonarqube-tests.yml` — tests y análisis SonarQube
- `.github/workflows/cd-quality-gate-docker-publish.yml` — quality gate y publicación
- `.github/workflows/wd-docker-image-builder.yml` — build manual de imagen
- `.github/workflows/auto-release.yml` — release

## Cosas que conviene saber antes de tocar nada

1. **`Core/`, `BusinessLogic`, `DataAccess` y `AppLogic.DevartDtos` no se modifican.** Los primeros
   son submódulo compartido; los otros, código generado por Devart.
2. **`dotnet build` regenera ~280 archivos de `AppLogic.DevartDtos`** con timestamp nuevo. Antes de
   commitear: `git diff --numstat -- WebApiAdmisiones/AppLogic/AppLogic.DevartDtos | awk '$1!="0"||$2!="0"'`.
3. **Los DTOs y las claves de query de `AppLogic.Integrations.*` van en español.** Modelan formatos
   de sistemas ajenos: traducirlos rompe la integración sin dar error de compilación.
4. **Un `sed -i` recursivo sobre `*.cs` es peligroso**: pisa código generado y literales de string
   (mensajes de error, URLs de APIs externas). Excluí `AppLogic.DevartDtos/`, `BusinessLogic/`,
   `DataAccess/` y `Core/`, y revisá después qué literales cambiaron.
5. **Si cambiás una ruta o sus params, actualizá `loadtest/get-endpoints.k6.js`** en el mismo cambio.
6. **`WebApiAdmisiones/Directory.Build.props` fija versiones parcheadas** de dependencias
   transitivas de `Core` con CVEs. Borralo cuando `Core` se actualice.
