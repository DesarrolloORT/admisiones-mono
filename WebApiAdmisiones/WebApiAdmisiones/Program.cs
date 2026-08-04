using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using WebApiAdmisiones.Extensions;
using WebApiAdmisiones.Security.Authentication;

// ============================================================================
// Program.cs
// Punto de arranque de la aplicación (top-level statements en .NET 9).
// Responsabilidades clave:
//  1. Configurar logging y telemetría (OpenTelemetry + Serilog).
//  2. Registrar servicios, filtros globales y dependencias de negocio.
//  3. Configurar autenticación / autorización JWT.
//  4. Definir el pipeline HTTP (middleware order) para seguridad, validación y métricas.
//  5. Exponer endpoints (controllers, métricas, swagger en desarrollo).
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------------------------------
// 1. Logging / Telemetría
// --------------------------------------------------------------------------
builder.Logging.ClearProviders();

builder.Services.AddOpenTelemetryConfiguration(builder.Configuration);
builder.Host.ConfigureSerilog(builder.Configuration);

// --------------------------------------------------------------------------
// 2. Kestrel Security Configuration
// --------------------------------------------------------------------------
builder.WebHost.ConfigureKestrelSecurity();

// --------------------------------------------------------------------------
// 3. Servicios de Aplicación
// --------------------------------------------------------------------------
builder.Services.AddApiControllers();

builder.Services.AddSwaggerGen(static options =>
{
    var xmlCommentsPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

    if (File.Exists(xmlCommentsPath))
    {
        options.IncludeXmlComments(xmlCommentsPath);
    }

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Admisiones",
        Version = "v1",
        Description = "La autenticación se realiza mediante cookies HttpOnly generadas al iniciar sesión."
    });

    options.OperationFilter<AuthDescriptionOperationFilter>();
});

// --------------------------------------------------------------------------
// 4. Servicios de Dominio
// --------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddDomainServices(builder.Configuration, builder.Environment);

// --------------------------------------------------------------------------
// 5. Cliente HTTP para API de Inscripciones y Pagos
// --------------------------------------------------------------------------
builder.Services.AddInscripcionesyPagosApiClient(builder.Configuration);

// --------------------------------------------------------------------------
// 6. CORS
// --------------------------------------------------------------------------
builder.Services.AddCorsPolicy();

// --------------------------------------------------------------------------
// 7. Redis para Rate Limiting Distribuido
// --------------------------------------------------------------------------
builder.Services.AddRedisRateLimiting();

// --------------------------------------------------------------------------
// 8. Rate Limiting (políticas específicas por endpoint)
// --------------------------------------------------------------------------
builder.Services.AddReconocimientoDocumentoRateLimiting(builder.Configuration);
builder.Services.AddLoginRateLimiting(builder.Configuration);

// --------------------------------------------------------------------------
// 9. Build Application
// --------------------------------------------------------------------------
var app = builder.Build();

// --------------------------------------------------------------------------
// 9. Configure HTTP Pipeline
// --------------------------------------------------------------------------
app.ConfigureMiddlewarePipeline(builder.Configuration);

app.Logger.LogInformation("Prueba básica: el sistema de logging está funcionando correctamente.");

await app.RunAsync();
