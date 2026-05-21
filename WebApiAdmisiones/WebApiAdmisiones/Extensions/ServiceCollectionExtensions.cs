using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AppLogic.DTOs;
using AzureService.DTOs;
using Prometheus;
using Sanitization.Code;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar servicios de la aplicación.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        private const string ReconocimientoDocumentoRateLimitPolicy = "ReconocimientoDocumento";
        private const string LoginRateLimitPolicy = "LoginAttempts";

        private static readonly Counter ReconocimientoDocumentoRateLimitRejections = Metrics.CreateCounter(
            "reconocimiento_documento_rate_limit_rejections_total",
            "Cantidad de solicitudes de reconocimiento de documento rechazadas por rate limit.");

        private static readonly Counter LoginRateLimitRejections = Metrics.CreateCounter(
            "login_rate_limit_rejections_total",
            "Cantidad de intentos de login rechazados por rate limit (protección contra fuerza bruta).");

        /// <summary>
        /// Configura los controladores MVC con filtros globales de seguridad y validación.
        /// </summary>
        public static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add<SanitizeAttribute>();
                options.Filters.Add<InputRedactionLoggingFilter>();
                options.Filters.Add<JsonSchemaValidationFilter>();
                options.ReturnHttpNotAcceptable = true;
                options.Filters.Add(new ProducesAttribute("application/json"));
            });

            services.AddEndpointsApiExplorer();

            // Registro de validación de esquemas
            services.AddSingleton<IJsonSchemaRegistry, InMemoryJsonSchemaRegistry>();
            services.AddScoped<JsonSchemaValidationFilter>();
            services.AddScoped<InputRedactionLoggingFilter>();
            services.AddScoped<RequireCaptchaFilter>();

            return services;
        }

        /// <summary>
        /// Configura CORS con los orígenes permitidos.
        /// </summary>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            var allowedOrigins = new[]
            {
                "http://localhost:4200",
                "http://localhost:5001/",

                "https://admisiones.ort.edu.uy",
                "https://admisionespreprod.ort.edu.uy",
                "https://admisionestesting.ort.edu.uy",
                "https://admisionesdesa.ort.edu.uy",
            };

            const string corsPolicy = "AllowAngularApp";

            services.AddCors(options =>
            {
                options.AddPolicy(name: corsPolicy, policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                          .AllowAnyHeader()
                          .AllowCredentials();  // Requerido: API usa cookies HttpOnly para tokens (X-Access-Token, X-Refresh-Token)
                });
            });

            return services;
        }

        public static IServiceCollection AddReconocimientoDocumentoRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var configuredLimit = configuration.GetValue<int?>("ReconocimientoDocumento:RateLimitPerMinute");
            var permitLimit = configuredLimit is > 0 ? configuredLimit.Value : 5;

            services.AddRateLimiter(options =>
            {
                options.AddPolicy(ReconocimientoDocumentoRateLimitPolicy, httpContext =>
                {
                    var userKey = httpContext.User?.Identity?.IsAuthenticated == true
                        ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          httpContext.User.FindFirst("sub")?.Value ??
                          httpContext.User.Identity?.Name
                        : null;

                    var partitionKey = !string.IsNullOrWhiteSpace(userKey)
                        ? $"user:{userKey}"
                        : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    ReconocimientoDocumentoRateLimitRejections.Inc();
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    var result = OperationResult<ReconocimientoDocumentoResponse>.IsFailed(
                        "REC_DOC_15",
                        ReconocimientoDocumentoRateLimitPolicy,
                        "Se superó el límite de solicitudes de reconocimiento de documentos. Intentá nuevamente en unos minutos.",
                        429,
                        default!);

                    await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken);
                };
            });

            return services;
        }

        /// <summary>
        /// Configura rate limiting para el endpoint de Login siguiendo estándares OWASP.
        /// Protege contra ataques de fuerza bruta limitando intentos por IP.
        /// </summary>
        /// <param name="services">Colección de servicios.</param>
        /// <param name="configuration">Configuración de la aplicación.</param>
        /// <returns>La colección de servicios para encadenamiento.</returns>
        /// <remarks>
        /// Configuración predeterminada (según OWASP Anti-Automation):
        /// - Límite: 5 intentos por 15 minutos por IP
        /// - Algoritmo: Sliding Window (más preciso que Fixed Window)
        /// - Bloqueo: Sin cola (rechazo inmediato al superar límite)
        /// - Respuesta: HTTP 429 con mensaje específico
        /// - Monitoreo: Métrica Prometheus para intentos bloqueados
        /// 
        /// Configurable vía appsettings.json:
        /// {
        ///   "Authentication": {
        ///     "Login": {
        ///       "RateLimitAttempts": 5,
        ///       "RateLimitWindowMinutes": 15
        ///     }
        ///   }
        /// }
        /// </remarks>
        public static IServiceCollection AddLoginRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configuración desde appsettings o valores por defecto (OWASP recomendado)
            var maxAttempts = configuration.GetValue<int?>("Authentication:Login:RateLimitAttempts") ?? 5;
            var windowMinutes = configuration.GetValue<int?>("Authentication:Login:RateLimitWindowMinutes") ?? 15;

            services.AddRateLimiter(options =>
            {
                options.AddPolicy(LoginRateLimitPolicy, httpContext =>
                {
                    // Login es SIEMPRE anónimo, usamos solo IP
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var partitionKey = $"login-ip:{ipAddress}";

                    // Sliding Window: más estricto que Fixed Window
                    // Ventaja: si haces 5 intentos en el minuto 0, no se resetea en el minuto 1,
                    // sino que cada intento "expira" 15 minutos después de hacerse
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey,
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = maxAttempts,
                            Window = TimeSpan.FromMinutes(windowMinutes),
                            SegmentsPerWindow = 3,  // Divide la ventana en 3 segmentos (más preciso)
                            QueueLimit = 0,          // Sin cola, rechazo inmediato
                            AutoReplenishment = true
                        });
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    LoginRateLimitRejections.Inc();
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    var result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "AUTH_RL_01",
                        LoginRateLimitPolicy,
                        $"Se superó el límite de intentos de inicio de sesión ({maxAttempts} intentos cada {windowMinutes} minutos). Por tu seguridad, intentá nuevamente más tarde.",
                        429,
                        default!);

                    await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken);
                };
            });

            return services;
        }
    }
}
