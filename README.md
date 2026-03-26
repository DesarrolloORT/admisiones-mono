# API Admisiones

API REST de admisiones de ORT Uruguay construida sobre `.NET 10`. La solucion expone endpoints para autenticacion, catalogos, persona, inscripciones, preinscripcion, becas y fondo de beca.

## Estructura del repositorio

```text
api-admisiones/
|-- Core/                          Submodulo compartido
|-- WebApiAdmisiones/
|   |-- AppLogic/                 Servicios, DTOs, helpers y contratos
|   |-- BusinessLogic/            Entidades e interfaces de repositorios
|   |-- DataAccess/               Contextos y repositorios Devart/Oracle
|   |-- UnitTesting/              Suite xUnit + Moq + cobertura
|   `-- WebApiAdmisiones/         Proyecto web, controllers y pipeline HTTP
`-- .github/workflows/            CI, SonarQube y build de imagen
```

## Stack principal

- `.NET 10`
- `ASP.NET Core Web API`
- `Entity Framework Core` con proveedor `Devart` para Oracle
- `JWT Bearer` + cookies HttpOnly para refresh token
- `Swagger / Swashbuckle`
- `Serilog`, `OpenTelemetry` y `prometheus-net`
- `xUnit`, `Moq` y `coverlet.collector`

## Controllers principales

- `LoginController`
- `CatalogosController`
- `InscripcionesController`
- `PreinscripcionController`
- `PersonaController`
- `BecasController`
- `FondoDeBecaController`

## Requisitos

- `.NET SDK 10`
- acceso al repositorio con submodulos
- acceso a Oracle segun el ambiente que se quiera usar
- variables de entorno para autenticacion y conexion

## Clonado

```bash
git clone --recurse-submodules git@github.com:DesarrolloORT/api-admisiones.git
cd api-admisiones
```

Si el repo ya fue clonado sin submodulos:

```bash
git submodule update --init --recursive
```

## Restore y build

Desde la raiz del repo:

```bash
dotnet restore WebApiAdmisiones/WebApiAdmisiones.sln
dotnet build WebApiAdmisiones/WebApiAdmisiones.sln -c Debug
```

## Configuracion local

La API toma la conexion Oracle desde variable de entorno:

```powershell
$env:OracleConnectionStringAdmisiones="Data Source=...;User Id=...;Password=..."
```

Para autenticacion y emision de tokens, hoy se usan estas variables:

```powershell
$env:JWT_SECRET_KEY="secret"
$env:JWT_EXPIRE_MINUTES_ADMISIONES="60"
$env:JWT_REFRESH_EXPIRE_ADMISIONES="1440"
$env:JWT_ISSUER_TOKEN_ADMISIONES="https://webapiadmisiones.ort.edu.uy"
$env:JWT_AUDIENCE_TOKEN_ADMISIONES="https://webapiadmisiones.ort.edu.uy"
```

Tambien hay configuracion por ambiente en:

- `WebApiAdmisiones/WebApiAdmisiones/appsettings.json`
- `WebApiAdmisiones/WebApiAdmisiones/appsettings.Development.json`
- `WebApiAdmisiones/WebApiAdmisiones/appsettings.LocalHost.json`
- `WebApiAdmisiones/WebApiAdmisiones/appsettings.Testing.json`
- `WebApiAdmisiones/WebApiAdmisiones/appsettings.Preproduction.json`
- `WebApiAdmisiones/WebApiAdmisiones/appsettings.Production.json`

La logica de ambientes productivos esta documentada en [ENVIRONMENT_CONFIGURATION.md](C:/GIT/api-admisiones/WebApiAdmisiones/WebApiAdmisiones/Extensions/ENVIRONMENT_CONFIGURATION.md).

## Ejecutar la API

Perfil HTTP local:

```bash
dotnet run --project WebApiAdmisiones/WebApiAdmisiones/WebApiAdmisiones.csproj --launch-profile http
```

Perfil HTTPS de desarrollo:

```bash
dotnet run --project WebApiAdmisiones/WebApiAdmisiones/WebApiAdmisiones.csproj --launch-profile https
```

URLs por defecto:

- `http://localhost:8080/swagger`
- `https://localhost:7150/swagger`

Swagger queda habilitado en `Development` y `Testing`. Los ambientes `Preproduction` y `Production` se comportan como ambientes productivos.

## Tests

Ejecutar toda la suite:

```bash
dotnet test WebApiAdmisiones/UnitTesting/UnitTesting.csproj
```

Ejecutar con cobertura:

```bash
dotnet test WebApiAdmisiones/UnitTesting/UnitTesting.csproj --settings WebApiAdmisiones/UnitTesting/tests.runsettings --collect:"XPlat Code Coverage"
```

La configuracion de cobertura genera reportes `cobertura` y `opencover` dentro de `WebApiAdmisiones/UnitTesting/TestResults/`.

## CI y calidad

Workflows principales:

- `.github/workflows/ci-sonarqube-tests.yml`: tests y analisis de SonarQube
- `.github/workflows/cd-quality-gate-docker-publish.yml`: pipeline de calidad y publicacion
- `.github/workflows/wd-docker-image-builder.yml`: build manual de imagen
- `.github/workflows/auto-release.yml`: automatizacion de release

## Notas

- `Core/` es un submodulo y forma parte obligatoria del build.
- La suite de `UnitTesting` cubre controllers, services, extensiones, seguridad y utilidades.
- El proyecto web vive en `WebApiAdmisiones/WebApiAdmisiones/`.
