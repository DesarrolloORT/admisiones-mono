# Instrucciones para GitHub Copilot - API Ficha de Persona (FDP)

## Proyecto

API RESTful en **.NET 9** para gestión de fichas de personas. Stack: EF Core 9, JWT auth, xUnit/Moq, Docker, Prometheus, Serilog.

## ⚠️ Prerrequisito Crítico: Submódulo Core

El submódulo privado `/Core` contiene bibliotecas compartidas. **Sin él, build/restore fallan.**

```bash
git submodule status           # Verificar estado
git submodule update --init --recursive  # Inicializar (requiere SUBMODULES_TOKEN en CI)
```

**Error típico:** `MSB3202: The project file ... was not found` → Submódulo no inicializado.

## Arquitectura

```
FichaDePersona/
├── WebApiFDP/         # API REST (Controllers, Security, Program.cs)
├── AppLogic/          # Servicios, DTOs, Helpers
├── BusinessLogic/     # Entidades, interfaces de repositorio
├── DataAccess/        # Implementaciones EF Core, repositorios
└── UnitTesting/       # xUnit + Moq

Core/                  # Submódulo privado
├── Utilities/         # OperationResult<T>, validadores, constantes
├── DbConnectionContext/
├── MailORT/
└── Modules/           # ModBandeja, ModGenericBase
```

## Comandos Esenciales

```bash
cd FichaDePersona

# Build
dotnet restore WebApiFDP.sln
dotnet build WebApiFDP.sln --configuration Release --no-restore

# Tests
dotnet test UnitTesting/UnitTesting.csproj --settings UnitTesting/tests.runsettings

# Docker (desde raíz del repo)
cd ..
docker build -f FichaDePersona/WebApiFDP/Dockerfile -t api-fdp:local .
```

## Patrón OperationResult<T> (Obligatorio)

Todos los servicios retornan `OperationResult<T>`. Ver `Core/Utilities/OperationResult.cs`:

```csharp
// Éxito
return OperationResult<DtoEntity>.Ok(dto, nameof(MetodoActual));

// Error con código HTTP
return OperationResult<DtoEntity>.IsFailed(
    errorCode: "FDP_GIP_01",      // Formato: FDP_[SIGLAS]_[NN]
    originMethod: nameof(MetodoActual),
    message: "Descripción del error",
    httpCode: 400);               // 400, 404, 409, 422
```

**Códigos HTTP estándar:** 200 OK, 400 Bad Request, 404 Not Found, 409 Conflict, 422 Unprocessable Entity

## Código Autogenerado (NO MODIFICAR)

Archivos generados por Devart Entity Developer que **serán sobrescritos**:
- `**/DevartDTOs/**`, `**/DevartEFCore/**`, `**/DevartDataAccess/**`
- Cualquier archivo `*.Generated.cs`

## Patrón Unit of Work

```csharp
public OperationResult<DtoEntidad> MetodoServicio(long id)
{
    using var uow = _uowFactory.Create();  // Siempre usar 'using'
    var entity = uow.MiRepositorio.GetByKey(id);
    // ... lógica
    uow.Save();  // Para escrituras
    return OperationResult<DtoEntidad>.Ok(dto, nameof(MetodoServicio));
}
```

## Tests Unitarios

Patrón estándar con xUnit + Moq (ver `UnitTesting/Controllers/`):

```csharp
public class MiControllerTests
{
    private readonly Mock<IMiService> _serviceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    
    [Fact]
    public void MiMetodo_ReturnsOk()
    {
        _serviceMock.Setup(s => s.MiMetodo())
            .Returns(OperationResult<MiDto>.Ok(new MiDto(), "MiMetodo"));
        // Act & Assert...
    }
}
```

## Seguridad: Middlewares y Filtros

El orden en `MiddlewarePipelineExtensions.cs` es **crítico**. Filtros globales activos:
- `SanitizeAttribute` - Prevención XSS
- `InputRedactionLoggingFilter` - Redacción de datos sensibles
- `JsonSchemaValidationFilter` - Validación de JSON

**NO remover filtros sin justificación de seguridad.**

## CI/CD

- **`build-publish.yml`**: Build Docker → Push a `ghcr.io/desarrolloort/api-fdp`
- **`sonarQube&TestCoverageWebApiFDP.yml`**: Tests + cobertura + SonarQube

Ambos requieren `SUBMODULES_TOKEN` para clonar el submódulo Core.

## Validaciones Pre-Commit

1. `dotnet restore FichaDePersona/WebApiFDP.sln`
2. `dotnet build FichaDePersona/WebApiFDP.sln --configuration Release --no-restore`
3. `dotnet test FichaDePersona/UnitTesting/UnitTesting.csproj`

---

**Archivos clave:** `Program.cs`, `ApiBaseController.cs`, `MiddlewarePipelineExtensions.cs`, `OperationResult.cs`
