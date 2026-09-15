# Configuración de Ambientes - Production vs Preproduction

## Resumen

Este documento describe cómo los ambientes **Production** y **Preproduction** están configurados para comportarse de manera **idéntica** en términos de seguridad, logging y configuraciones sensibles.

## Método de Extensión: `IsProductionLike()`

### Ubicación
`WebApiAdmisiones/Extensions/EnvironmentExtensions.cs`

### Definición
```csharp
public static bool IsProductionLike(this IHostEnvironment environment)
{
    return environment.IsProduction() || environment.IsEnvironment("Preproduction");
}
```

### Propósito
Agrupar los ambientes Production y Preproduction para aplicar las mismas configuraciones de seguridad y privacidad.

## Comportamiento por Ambiente

| Característica | Development | Testing | Preproduction | Production |
|----------------|-------------|---------|---------------|------------|
| **Swagger UI** | ? Habilitado | ? Habilitado | ? Deshabilitado | ? Deshabilitado |
| **DB Logging Detallado** | ? Habilitado | ? Habilitado | ? Deshabilitado | ? Deshabilitado |
| **Sensitive Data Logging** | ? Habilitado | ? Habilitado | ? Deshabilitado | ? Deshabilitado |
| **Stack Traces en Errores** | ? Completos | ? Completos | ? Enmascarados | ? Enmascarados |
| **Detalles de Validación** | ? ProblemDetails | ? ProblemDetails | ? Genéricos | ? Genéricos |
| **HSTS** | ? Deshabilitado | ? Habilitado | ? Habilitado | ? Habilitado |

## Archivos Afectados

### 1. `ExceptionHandlingMiddleware.cs`
```csharp
// ANTES
var result = OperationResult<Exception>.IsFailed(
    "INTERNAL_ERROR", 
    nameof(ExceptionHandlingMiddleware),
    "Error inesperado", 
    StatusCodes.Status500InternalServerError, 
    !_env.IsProduction() ? ex : null);  // ? Solo ocultaba en Production

// DESPUÉS
var result = OperationResult<Exception>.IsFailed(
    "INTERNAL_ERROR", 
    nameof(ExceptionHandlingMiddleware),
    "Error inesperado", 
    StatusCodes.Status500InternalServerError, 
    !_env.IsProductionLike() ? ex : null);  // ? Oculta en Production y Preproduction
```

### 2. `ModelBindingErrorLoggingMiddleware.cs`
```csharp
// ANTES
if (_environment.IsProduction())
    await HandleBadRequestNonDevelopmentAsync(...);  // ? Solo Production
else
    await HandleBadRequestDevelopmentAsync(...);

// DESPUÉS
if (_environment.IsProductionLike())
    await HandleBadRequestProductionLikeAsync(...);  // ? Production y Preproduction
else
    await HandleBadRequestDevelopmentAsync(...);
```

### 3. `DomainServicesExtensions.cs`
```csharp
// ANTES
if (!environment.IsProduction())
{
    options.EnableSensitiveDataLogging()
           .EnableDetailedErrors();  // ? Habilitado en Preproduction
}

// DESPUÉS
if (!environment.IsProductionLike())
{
    options.EnableSensitiveDataLogging()
           .EnableDetailedErrors();  // ? Deshabilitado en Preproduction
}
```

### 4. `MiddlewarePipelineExtensions.cs`
```csharp
// ANTES
if (!app.Environment.IsProduction() && !app.Environment.IsEnvironment("Preproduction"))
{
    app.UseSwagger();  // ? Lógica duplicada
}

// DESPUÉS
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();  // ? Más claro y explícito
}
```

## Tests Actualizados

### `ModelBindingErrorLoggingMiddlewareTests.cs`

Se agregaron tests específicos para validar el comportamiento de Preproduction:

```csharp
[Fact]
public async Task Invoke_MasksErrorsInPreproduction()
{
    // Arrange
    var environment = CreateMockEnvironment("Preproduction");
    
    // Act & Assert
    // Debe enmascarar errores igual que Production
}

[Fact]
public async Task Invoke_PreservesOperationResultInPreproduction()
{
    // Arrange
    var environment = CreateMockEnvironment("Preproduction");
    
    // Act & Assert
    // Debe preservar OperationResult igual que Production
}

[Fact]
public async Task Invoke_TransformsProblemDetailsToOperationResult_InTesting()
{
    // Arrange
    var environment = CreateMockEnvironment("Testing");
    
    // Act & Assert
    // Debe transformar ProblemDetails igual que Development
}
```

## Validación de Cambios

### Checklist de Configuraciones

- [x] **ExceptionHandlingMiddleware**: Usa `IsProductionLike()` para ocultar excepciones
- [x] **ModelBindingErrorLoggingMiddleware**: Usa `IsProductionLike()` para enmascarar errores
- [x] **DomainServicesExtensions**: Usa `IsProductionLike()` para deshabilitar DB logging sensible
- [x] **MiddlewarePipelineExtensions**: Swagger deshabilitado en Production y Preproduction
- [x] **MiddlewarePipelineExtensions**: HSTS habilitado excepto en Development
- [x] **Tests**: Cobertura de Preproduction agregada

### Comando de Verificación

```bash
# Build exitoso
dotnet build WebApiAdmisiones/WebApiAdmisiones.csproj --configuration Release

# Tests pasan
dotnet test UnitTesting/UnitTesting.csproj --filter "FullyQualifiedName~ModelBindingErrorLoggingMiddleware"
```

## Beneficios

1. **Seguridad Consistente**: Production y Preproduction tienen el mismo nivel de protección
2. **Debugging Facilitado**: Development y Testing mantienen información detallada para desarrollo
3. **Código Más Limpio**: Un solo método `IsProductionLike()` en lugar de lógica duplicada
4. **Mantenibilidad**: Cambios futuros solo requieren actualizar un método
5. **Testing**: Comportamiento explícito y validado para cada ambiente

## Migración de Código Legacy

Si encuentras código que aún usa `!IsProduction()`, reemplázalo con:

```csharp
// ? INCORRECTO - No considera Preproduction
if (!environment.IsProduction())
{
    // Configuración de desarrollo
}

// ? CORRECTO - Considera Preproduction como productivo
if (!environment.IsProductionLike())
{
    // Configuración de desarrollo
}
```

## Referencias

- **Archivo principal**: `WebApiAdmisiones/Extensions/EnvironmentExtensions.cs`
- **Tests**: `UnitTesting/Security/ModelBindingErrorLoggingMiddlewareTests.cs`
- **Documentación general**: `WebApiAdmisiones/Extensions/README_REFACTORING.md`
