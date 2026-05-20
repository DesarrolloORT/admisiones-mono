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
        private static readonly Counter ReconocimientoDocumentoRateLimitRejections = Metrics.CreateCounter(
            "reconocimiento_documento_rate_limit_rejections_total",
            "Cantidad de solicitudes de reconocimiento de documento rechazadas por rate limit.");

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
                          //.WithHeaders("authorization", "content-type", "x-request-id", "x-token")
                          .AllowAnyHeader()
                          .AllowCredentials();
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
    }
}
