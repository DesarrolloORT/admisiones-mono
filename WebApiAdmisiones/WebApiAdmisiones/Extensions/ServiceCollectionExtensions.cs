using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AppLogic.DTOs;
using AzureService.DTOs;
using Prometheus;
using Sanitization.Code;
using Utilities;
using WebApiAdmisiones.Security;
using StackExchange.Redis;

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
            "Cantidad de intentos de login rechazados por rate limit por IP (protección contra credential stuffing).");

        // Métrica para rate limiting por cuenta (IP + Documento)
        // Se incrementa en AuthController.Login() cuando se excede el límite de una cuenta específica
        public static readonly Counter LoginAccountRateLimitRejections = Metrics.CreateCounter(
            "login_account_rate_limit_rejections_total",
            "Cantidad de intentos de login rechazados por rate limit de cuenta (IP+Documento, protección contra ataques distribuidos).");

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
        /// Configura la conexión a Redis para rate limiting distribuido.
        /// </summary>
        /// <param name="services">Colección de servicios.</param>
        /// <param name="configuration">Configuración de la aplicación.</param>
        /// <returns>La colección de servicios para encadenamiento.</returns>
        public static IServiceCollection AddRedisRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configurar conexión a Redis
            var redisConnection = configuration.GetConnectionString("Redis");

            if (string.IsNullOrWhiteSpace(redisConnection))
            {
                throw new InvalidOperationException(
                    "Redis connection string is required for rate limiting. " +
                    "Add 'ConnectionStrings:Redis' to appsettings.json");
            }

            // Registrar IConnectionMultiplexer como singleton
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IConnectionMultiplexer>>();

                try
                {
                    var options = ConfigurationOptions.Parse(redisConnection);
                    options.AbortOnConnectFail = false; // No abortar si falla la primera conexión
                    options.ConnectTimeout = 5000;      // 5 segundos timeout
                    options.SyncTimeout = 5000;
                    options.ReconnectRetryPolicy = new ExponentialRetry(5000); // Reintentos exponenciales

                    var connection = ConnectionMultiplexer.Connect(options);

                    // Eventos para logging
                    connection.ConnectionFailed += (sender, args) =>
                    {
                        logger.LogError("Redis connection failed: {EndPoint} - {FailureType}", 
                            args.EndPoint, args.FailureType);
                    };

                    connection.ConnectionRestored += (sender, args) =>
                    {
                        logger.LogInformation("Redis connection restored: {EndPoint}", args.EndPoint);
                    };

                    connection.ErrorMessage += (sender, args) =>
                    {
                        logger.LogError("Redis error: {Message}", args.Message);
                    };

                    logger.LogInformation("Redis connection established successfully to {Endpoints}", 
                        string.Join(", ", connection.GetEndPoints()));

                    return connection;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to connect to Redis. Rate limiting will fail-open.");
                    throw;
                }
            });

            // Registrar servicio de rate limiting
            services.AddSingleton<IRedisRateLimiterService, RedisRateLimiterService>();

            // Registrar servicio de cache distribuido
            services.AddSingleton<IRedisCacheService, RedisCacheService>();

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
                          // El frontend agrega traceparent/baggage y headers X-Client-* de telemetría.
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
        /// Implementa protección DUAL contra ataques de fuerza bruta.
        /// </summary>
        /// <param name="services">Colección de servicios.</param>
        /// <param name="configuration">Configuración de la aplicación.</param>
        /// <returns>La colección de servicios para encadenamiento.</returns>
        /// <remarks>
        /// ⚠️ PROTECCIÓN DUAL ADAPTATIVA (OWASP Anti-Automation):
        /// 
        /// 1️⃣ Rate Limit POR IP SOLAMENTE (este middleware):
        ///    - Límite: 10 intentos por 15 minutos por IP
        ///    - Previene: Credential stuffing masivo (probar muchas cuentas desde una IP)
        ///    - Se ejecuta: ANTES de deserializar el body (middleware)
        ///    - Permite: Múltiples usuarios legítimos en la misma red/PC pública
        /// 
        /// 2️⃣ Rate Limit POR IP+DOCUMENTO (en el controlador):
        ///    - Límite: 5 intentos por 15 minutos por combinación IP+Documento
        ///    - Previene: Ataque focalizado a una cuenta específica
        ///    - Se ejecuta: DESPUÉS de deserializar el body (en AuthController.Login)
        ///    - Bloquea: Intentos repetidos al mismo usuario desde la misma IP
        /// 
        /// 🎯 Escenarios cubiertos:
        /// - Usuario legítimo olvidó password → Máx 5 intentos con su documento
        /// - Familia/oficina con IP compartida → Hasta 10 usuarios diferentes OK
        /// - Ataque a cuenta específica → Bloqueado al 5to intento (IP+Doc)
        /// - Credential stuffing masivo → Bloqueado al 10mo intento (solo IP)
        /// 
        /// Configuración (valores por defecto):
        /// - Algoritmo: Sliding Window (más preciso que Fixed Window)
        /// - Backend: Redis distribuido (estado compartido entre instancias)
        /// - Respuesta: HTTP 429 con headers estándar (X-RateLimit-*, Retry-After)
        /// - Monitoreo: Métricas Prometheus separadas por tipo
        /// 
        /// Configurable vía appsettings.json:
        /// {
        ///   "Authentication": {
        ///     "Login": {
        ///       "RateLimitIpAttempts": 10,              // Solo IP
        ///       "RateLimitAccountAttempts": 5,          // IP + Documento
        ///       "RateLimitWindowMinutes": 15
        ///     }
        ///   }
        /// }
        /// 
        /// Ventajas de Redis sobre in-memory:
        /// - Estado compartido entre múltiples instancias de la API
        /// - Persistencia ante reinicios
        /// - Mayor precisión en ambientes distribuidos
        /// 
        /// 🔐 Seguridad:
        /// Ambos límites deben pasar para que el request sea procesado.
        /// Si cualquiera de los dos falla → HTTP 429 Too Many Requests
        /// </remarks>
        public static IServiceCollection AddLoginRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Límite por IP solamente (más permisivo para IPs compartidas)
            var maxIpAttempts = configuration.GetValue<int?>("Authentication:Login:RateLimitIpAttempts") ?? 10;
            var windowMinutes = configuration.GetValue<int?>("Authentication:Login:RateLimitWindowMinutes") ?? 15;

            services.AddRateLimiter(options =>
            {
                options.AddPolicy(LoginRateLimitPolicy, httpContext =>
                {
                    // Login es SIEMPRE anónimo, usamos solo IP
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var partitionKey = $"login-ip:{ipAddress}";

                    // Usar Redis Rate Limiter en lugar de in-memory
                    var redisService = httpContext.RequestServices.GetRequiredService<IRedisRateLimiterService>();

                    return RateLimitPartition.Get(
                        partitionKey,
                        key => new RedisRateLimiter(
                            redisService,
                            key,
                            maxIpAttempts,  // 10 intentos (múltiples usuarios en misma IP)
                            TimeSpan.FromMinutes(windowMinutes)));
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    LoginRateLimitRejections.Inc();

                    // Obtener información adicional desde Redis
                    var redisService = context.HttpContext.RequestServices.GetRequiredService<RedisRateLimiterService>();
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var partitionKey = $"login-ip:{ipAddress}";

                    var remaining = await redisService.GetRemainingAsync(
                        partitionKey, 
                        maxIpAttempts, 
                        TimeSpan.FromMinutes(windowMinutes));

                    var resetTime = await redisService.GetResetTimeAsync(
                        partitionKey, 
                        TimeSpan.FromMinutes(windowMinutes));

                    // Headers estándar de rate limiting (RFC 6585 + draft IETF)
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.Headers["X-RateLimit-Limit"] = maxIpAttempts.ToString();
                    context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

                    if (resetTime.HasValue)
                    {
                        context.HttpContext.Response.Headers["X-RateLimit-Reset"] = 
                            resetTime.Value.ToUnixTimeSeconds().ToString();
                        context.HttpContext.Response.Headers["Retry-After"] = 
                            ((int)(resetTime.Value - DateTimeOffset.UtcNow).TotalSeconds).ToString();
                    }

                    var result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "AUTH_RL_01",
                        LoginRateLimitPolicy,
                        $"Se superó el límite de intentos de inicio de sesión desde esta red ({maxIpAttempts} intentos cada {windowMinutes} minutos). Por tu seguridad, intentá nuevamente más tarde.",
                        429,
                        default!);

                    await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken);
                };
            });

            return services;
        }
    }
}
