---
name: project-map
description: "Mapa estructural del repositorio para navegacion rapida del agente."
applyTo: "**"
---

# Project Map

```
api-admisiones/
├── AGENTS.md                          # AI baseline + perfiles de agente
├── CLAUDE.md                          # Idem para Claude
├── README.md
├── Core/                              # Submódulo privado — bibliotecas compartidas
│   ├── AzureService/                  # Reconocimiento de documentos (Azure AI)
│   │   ├── DTOs/
│   │   ├── Helpers/
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── DbConnectionContext/           # IDbConnectionContext, ConnectionContext
│   ├── LdapService/                   # Autenticación LDAP
│   ├── MailORT/                       # Envío de correos institucionales
│   ├── Modules/
│   │   ├── ModBandeja/
│   │   └── ModGenericBase/            # Unit of Work genérico
│   └── Utilities/                     # OperationResult<T>, Constantes, Encriptador, FileValidator, Util
│
└── WebApiAdmisiones/
    ├── WebApiAdmisiones.sln
    ├── WebApiAdmisiones/              # Proyecto API — entrypoint
    │   ├── Program.cs                 # Host + DI + pipeline
    │   ├── Controllers/
    │   │   ├── ApiBaseController.cs   # Base con helpers OperationResult → IActionResult
    │   │   ├── AuthController.cs
    │   │   ├── BecasController.cs
    │   │   ├── CatalogosController.cs
    │   │   ├── EjemploOfertasController.cs
    │   │   ├── FondoDeBecaController.cs
    │   │   ├── InscripcionesController.cs
    │   │   ├── PersonaController.cs
    │   │   ├── PreinscripcionController.cs
    │   │   └── RegistroController.cs
    │   ├── Extensions/
    │   │   ├── DomainServicesExtensions.cs    # Registro de servicios de dominio
    │   │   ├── EnvironmentExtensions.cs
    │   │   ├── HttpClientExtensions.cs
    │   │   ├── KestrelExtensions.cs
    │   │   ├── MiddlewarePipelineExtensions.cs  # Orden crítico del pipeline (XSS, redacción, validación JSON)
    │   │   ├── ServiceCollectionExtensions.cs
    │   │   └── TelemetryExtensions.cs
    │   ├── Security/
    │   │   ├── Authentication/        # JWT bearer config
    │   │   ├── Cache/
    │   │   ├── Captcha/
    │   │   ├── Middleware/            # SanitizeAttribute, InputRedactionLoggingFilter
    │   │   ├── Observability/
    │   │   ├── RateLimiting/
    │   │   └── RequestValidation/    # JsonSchemaValidationFilter
    │   ├── Helpers/
    │   │   └── FormFileHelper.cs
    │   ├── Observability/
    │   │   └── ClientTelemetryHeaders.cs
    │   ├── HttpHandlers/
    │   ├── Models/
    │   ├── Docs/
    │   └── appsettings*.json          # Entornos: Development, LocalHost, Testing, Preproduction, Production
    │
    ├── AppLogic/                      # Servicios de aplicación, DTOs, helpers
    │   ├── IServices/
    │   │   ├── Autenticacion/
    │   │   ├── Becas/
    │   │   ├── Catalogos/
    │   │   ├── Inscripciones/
    │   │   ├── Personas/
    │   │   ├── Registro/
    │   │   ├── IEmailSender.cs
    │   │   └── IRateLimiterService.cs
    │   ├── Services/
    │   │   ├── Autenticacion/
    │   │   ├── Becas/
    │   │   ├── Catalogos/
    │   │   ├── Email/
    │   │   ├── Inscripciones/
    │   │   ├── Personas/
    │   │   ├── RateLimiting/
    │   │   └── Registro/
    │   ├── DTOs/                      # Request/Response DTOs (Auth, Persona, Inscripcion, Registro, Becas...)
    │   ├── Helpers/                   # Validaciones, factories, builders de entidades
    │   ├── ApiClients/
    │   │   └── InscripcionesyPagosApiClient.cs  # Cliente HTTP externo
    │   ├── Constants/
    │   ├── DevartDTOs/                # ⚠️ Generado — NO modificar
    │   └── Utilities/
    │
    ├── BusinessLogic/                 # Entidades EF Core + interfaces de repositorio
    │   ├── DevartEFCore/              # ⚠️ Generado — NO modificar
    │   ├── IGenericRepository/        # Interfaces Unit of Work y repositorios genéricos
    │   └── IServices/
    │       └── IRefreshTokenService.cs
    │
    ├── DataAccess/                    # Implementaciones de repositorios EF Core
    │   ├── DevartDataAccess/          # ⚠️ Generado — NO modificar
    │   ├── GenericRepository/         # Unit of Work concreto
    │   └── Services/
    │       └── RefreshTokenService.cs
    │
    ├── Extensions/                    # Extensiones transversales de solución
    │
    └── UnitTesting/                   # xUnit + Moq
        ├── Controllers/               # Tests por controller (AuthController, Becas, Catalogos...)
        ├── AppLogic/
        ├── Services/
        ├── Security/
        ├── Observability/
        ├── Extensions/
        ├── Modulos/
        ├── Utilities/
        └── tests.runsettings
```
