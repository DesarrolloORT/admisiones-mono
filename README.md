# API Admisiones – WebApi .NET 10

## Descripción
API REST para el proceso de admisiones de ORT Uruguay. Expone servicios de consulta y gestión de pre-inscripciones, productos, procesos, becas, reglamentos y datos personales, consumida por el portal de autoservicio estudiantil.

# Arquitectura Web API en .NET 10 con Entity Framework (EF Core + Devart)
🚀 API RESTful diseñada para gestionar el flujo completo de admisiones: desde la consulta de productos y procesos habilitados hasta el registro de pre-inscripciones, encuestas iniciales y validación de becas. Integra EF Core con el proveedor Devart para Oracle.

## Características Tecnológicas

- 🚀 **.NET 10**: Plataforma de última generación con máximo rendimiento.
- 🛠️ **EF Core + Devart Oracle**: ORM con proveedor Devart para base de datos Oracle, modelo generado con Entity Developer.
- 🌐 **API RESTful**: Endpoints documentados con Swagger y autenticación JWT Bearer.
- 🧩 **Arquitectura Limpia**: Separación estricta en capas (Core, AppLogic, BusinessLogic, DataAccess, WebApiAdmisiones).
- 🔒 **Seguridad**: JWT Bearer, Kestrel hardening, User Secrets y variables de entorno para credenciales.
- 🐳 **Docker**: Despliegue en contenedor Linux con configuración lista para CI/CD.
- 🧪 **Pruebas**: xUnit + Moq con proyecto `UnitTesting` incluido en la solución.

## Arquitectura de la Solución

La solución `WebApiAdmisiones.sln` está organizada en capas y proyectos independientes:

```graphql
├─ Core                          (submódulo git – reutilizable entre proyectos)
│   ├─ DbConnectionContext       (contexto de conexión con Bearer token provider)
│   ├─ MailORT                   (servicio de envío de correo)
│   ├─ Modules
│   │   ├─ ModBandeja            (módulo de bandeja de tareas)
│   │   └─ ModGenericBase        (repositorio genérico, UoW base, entidades comunes)
│   └─ Utilities                 (constantes, encriptado, OperationResult, validadores)
│
├─ BusinessLogic
│   ├─ DevartEFCore
│   │   ├─ DevartEntities        (entidades generadas por Devart Entity Developer)
│   │   └─ IDevartRepositories   (interfaces de repositorios y IUnitOfWork)
│   └─ IGenericRepository        (interfaz base IRepository<T>)
│
├─ DataAccess
│   ├─ DevartContext             (ModelContext – DbContext generado por Devart)
│   └─ DevartRepositories        (implementaciones de repositorios + EntityFrameworkUnitOfWork)
│
├─ AppLogic
│   ├─ DevartDTOs
│   │   ├─ DevartDTO             (DTOs generados por Devart)
│   │   └─ DevartConverters      (converters entidad ↔ DTO generados por Devart)
│   ├─ IServices                 (interfaces de servicios de aplicación)
│   ├─ Services                  (implementaciones de servicios de aplicación)
│   ├─ Helpers                   (helpers de negocio)
│   └─ Utilities                 (utilidades de capa AppLogic)
│
├─ WebApiAdmisiones
│   ├─ Controllers               (endpoints REST)
│   ├─ Extensions                (extensiones de DI, Serilog, OpenTelemetry, Kestrel)
│   ├─ Security                  (JWT, CurrentUser)
│   └─ Program.cs                (punto de entrada, pipeline HTTP)
│
└─ UnitTesting
    └─ Pruebas de integración y unitarias (xUnit, Moq)
```

## Endpoints migrados

| Controller | Endpoint | Descripción |
|---|---|---|
| `Auth` | `POST /Auth/Login` | Autenticación, devuelve JWT |
| `General` | `GET /General/Paises` | Listado de países |
| `General` | `GET /General/TipoDocumentos` | Tipos de documento |
| `General` | `GET /General/ProcesosHabilitadosPorProducto` | Procesos vigentes por producto |
| `General` | `GET /General/UltimaInscripcion` | Última inscripción del usuario |
| `General` | `GET /General/Persona` | Datos personales del usuario |
| `General` | `GET /General/DatosPreInscripcion` | Encuesta inicial / pre-inscripción |
| `General` | `GET /General/Turnos` | Turnos disponibles para un proceso |
| `General` | `GET /General/Bachilleratos` / `AnioBachiller` | Datos bachillerato |
| `General` | `GET /General/Instituciones` / `Universidades` | Instituciones educativas |
| `General` | `GET /General/FondosDeBecaPorNivel` | Becas disponibles |
| `General` | `GET /General/AceptacionReglamentoEstudiantil` | Estado de aceptación de reglamento |
| `General` | `GET /General/ProductoInteresPersona` | Productos de interés del usuario |
| `FondoDeBeca` | — | Gestión de fondos de beca |
| `ProcesoComienzo` | — | Gestión de procesos y comienzos |

## Clonar el repositorio (con submódulos)

```bash
git clone --recurse-submodules git@github.com:DesarrolloORT/api-admisiones.git
```

Si los submódulos no se cargaron al clonar:

```bash
cd api-admisiones
git submodule update --init --recursive
```

## Restaurar y compilar

```bash
cd WebApiAdmisiones
dotnet restore WebApiAdmisiones.sln
dotnet build WebApiAdmisiones.sln -c Debug
```

## Configuración local

La cadena de conexión y el JWT secret se manejan con **User Secrets**. Para configurar localmente:

```bash
cd WebApiAdmisiones/WebApiAdmisiones
dotnet user-secrets set "ConnectionStrings:OracleConnection" "Data Source=...;User Id=...;Password=..."
dotnet user-secrets set "Jwt:Key" "tu-clave-secreta"
```
