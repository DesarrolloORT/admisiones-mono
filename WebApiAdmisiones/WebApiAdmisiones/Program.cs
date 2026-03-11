using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using WebApiAdmisiones.Extensions;
using WebApiAdmisiones.Security;

// ============================================================================
// Program.cs
// Punto de arranque de la aplicaciÃ³n (top-level statements en .NET 9).
// Responsabilidades clave:
//  1. Configurar logging y telemetrÃ­a (OpenTelemetry + Serilog).
//  2. Registrar servicios, filtros globales y dependencias de negocio.
//  3. Configurar autenticaciÃ³n / autorizaciÃ³n JWT.
//  4. Definir el pipeline HTTP (middleware order) para seguridad, validaciÃ³n y mÃ©tricas.
//  5. Exponer endpoints (controllers, mÃ©tricas, swagger en desarrollo).
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------------------------------
// 1. Logging / TelemetrÃ­a
// --------------------------------------------------------------------------
builder.Logging.ClearProviders();

builder.Services.AddOpenTelemetryConfiguration(builder.Configuration);
builder.Host.ConfigureSerilog(builder.Configuration);

// --------------------------------------------------------------------------
// 2. Kestrel Security Configuration
// --------------------------------------------------------------------------
builder.WebHost.ConfigureKestrelSecurity();

// --------------------------------------------------------------------------
// 3. Servicios de AplicaciÃ³n
// --------------------------------------------------------------------------
builder.Services.AddApiControllers();

// Swagger + JWT Security
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Por favor ingrese el token JWT en el formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
});
});




// --------------------------------------------------------------------------
// 4. Servicios de Dominio
// --------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddDomainServices(builder.Configuration, builder.Environment);

// --------------------------------------------------------------------------
// 5. CORS
// --------------------------------------------------------------------------
builder.Services.AddCorsPolicy();

// --------------------------------------------------------------------------
// 6. Build Application
// --------------------------------------------------------------------------
var app = builder.Build();

// --------------------------------------------------------------------------
// 7. Configure HTTP Pipeline
// --------------------------------------------------------------------------
app.ConfigureMiddlewarePipeline(builder.Configuration);

app.Logger.LogInformation("Prueba bÃ¡sica: Â¡El sistema de logging estÃ¡ funcionando correctamente!");

await app.RunAsync();
