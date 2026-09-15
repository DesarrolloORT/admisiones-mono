using AppLogic.Authentication.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AzureService.DTOs;
using Prometheus;
using Utilities;
using StackExchange.Redis;
using AppLogic.Platform.RateLimiting;
using WebApiAdmisiones.Security.RateLimiting;
using WebApiAdmisiones.Security.Cache;
using WebApiAdmisiones.Security.RequestValidation;

namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar servicios de la aplicación.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>Partición de rate limit cuando la IP del request no se puede resolver.</summary>
        private const string UnknownIpPartition = "unknown";

        private const string ReconocimientoDocumentoRateLimitPolicy = "ReconocimientoDocumento";
        private const string LoginRateLimitPolicy = "LoginAttempts";
        private const string PhoneValidationRateLimitPolicy = "PhoneValidation";
        public const string PublicAuthRateLimitPolicy = "PublicAuthAttempts";

        private static readonly Counter ReconocimientoDocumentoRateLimitRejections = Metrics.CreateCounter(
            "reconocimiento_documento_rate_limit_rejections_total",
            "Cantidad de solicitudes de reconocimiento de documento rechazadas por rate limit.");

        private static readonly Counter PhoneValidationRateLimitRejections = Metrics.CreateCounter(
            "phone_validation_rate_limit_rejections_total",
            "Cantidad de validaciones de teléfono rechazadas por rate limit.");

        private static readonly Counter LoginRateLimitRejections = Metrics.CreateCounter(
            "login_rate_limit_rejections_total",
            "Cantidad de intentos de login rechazados por rate limit por IP (protección contra credential stuffing).");

        private static readonly Counter PublicAuthRateLimitRejections = Metrics.CreateCounter(
            "public_auth_rate_limit_rejections_total",
            "Cantidad de requests a endpoints públicos de auth/registro rechazados por rate limit (protección contra enumeración de cuentas).");

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

            return services;
        }

        /// <summary>
        /// Configura la conexión a Redis para rate limiting distribuido.
        /// </summary>
        /// <param name="services">Colección de servicios.</param>
        /// <returns>La colección de servicios para encadenamiento.</returns>
        public static IServiceCollection AddRedisRateLimiting(this IServiceCollection services)
        {
            // Configurar conexión a Redis
            var redisConnection = Environment.GetEnvironmentVariable("RedisConnectionStringAdmisiones");

            if (string.IsNullOrWhiteSpace(redisConnection))
            {
                throw new InvalidOperationException(
                    "Redis connection string is required for rate limiting. " +
                    "Set the RedisConnectionStringAdmisiones environment variable.");
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
                        if (logger.IsEnabled(LogLevel.Information))
                        {
                            logger.LogInformation("Redis connection restored: {EndPoint}", args.EndPoint);
                        }
                    };

                    connection.ErrorMessage += (sender, args) =>
                    {
                        logger.LogError("Redis error: {Message}", args.Message);
                    };

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Redis connection established successfully to {Endpoints}",
                            string.Join(", ", connection.GetEndPoints()));
                    }

                    return connection;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Failed to connect to Redis during multiplexer initialization.", ex);
                }
            });

            // Registrar servicio de rate limiting
            services.AddSingleton<AppLogic.Platform.RateLimiting.IRateLimiterService, RedisRateLimiterService>();

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
                "http://localhost:5001",

                "https://admisiones.ort.edu.uy",
                "https://admisionespreprod.ort.edu.uy",
                "https://admisionestesting.ort.edu.uy",
                "https://admisionesdesa.ort.edu.uy",
                "https://admisionesdesa2.ort.edu.uy",
                "https://admisionestesting2.ort.edu.uy"
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

        public static IServiceCollection AddDocumentRecognitionRateLimiting(
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
                        : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownIpPartition}";

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

                // OnRejected NO se asigna acá: RateLimiterOptions es compartido entre todas las
                // políticas registradas con AddRateLimiter, y la última asignación pisa a las
                // anteriores. El dispatch único vive en AddLoginRateLimiting.
            });

            return services;
        }

        private static async Task HandleDocumentRecognitionRejected(
            OnRejectedContext context, CancellationToken cancellationToken)
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
        }

        /// <summary>
        /// Configura rate limiting para la validación de teléfonos, que es anónima.
        /// </summary>
        /// <param name="services">Colección de servicios.</param>
        /// <param name="configuration">Configuración de la aplicación.</param>
        /// <returns>La colección de servicios para encadenamiento.</returns>
        /// <remarks>
        /// El caso de uso es puro (libphonenumber en memoria, sin base ni servicios externos), así
        /// que el único abuso posible es el flood. Límite holgado: el front valida al salir de cada
        /// campo de teléfono (principal y alternativo) y no debe comerse un 429 tipeando.
        /// Configurable con "PhoneValidation:RateLimitPerMinute".
        /// </remarks>
        public static IServiceCollection AddPhoneValidationRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var configuredLimit = configuration.GetValue<int?>("PhoneValidation:RateLimitPerMinute");
            var permitLimit = configuredLimit is > 0 ? configuredLimit.Value : 20;

            services.AddRateLimiter(options =>
            {
                options.AddPolicy(PhoneValidationRateLimitPolicy, httpContext =>
                {
                    var userKey = httpContext.User?.Identity?.IsAuthenticated == true
                        ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          httpContext.User.FindFirst("sub")?.Value ??
                          httpContext.User.Identity?.Name
                        : null;

                    var partitionKey = !string.IsNullOrWhiteSpace(userKey)
                        ? $"phone-user:{userKey}"
                        : $"phone-ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownIpPartition}";

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

            });

            return services;
        }

        private static async Task HandlePhoneValidationRejected(
            OnRejectedContext context, CancellationToken cancellationToken)
        {
            PhoneValidationRateLimitRejections.Inc();
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            var result = OperationResult<bool>.IsFailed(
                "PERS_RL_01",
                PhoneValidationRateLimitPolicy,
                "Se superó el límite de validaciones de teléfono. Intentá nuevamente en un minuto.",
                429,
                default!);

            await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken);
        }

        /// <summary>Límite y ventana de la política de endpoints públicos de auth/registro.</summary>
        /// <remarks>
        /// Lo leen tanto el registro de la política como el handler de rechazo, así que vive acá
        /// para que no puedan desincronizarse y reportar headers que no corresponden al límite real.
        /// </remarks>
        private static (int Limit, TimeSpan Window) GetPublicAuthRateLimit(IConfiguration configuration)
        {
            var limit = configuration.GetValue<int?>("Authentication:PublicAuth:RateLimitPerWindow") ?? 30;
            var windowMinutes = configuration.GetValue<int?>("Authentication:PublicAuth:RateLimitWindowMinutes") ?? 15;

            return (limit, TimeSpan.FromMinutes(windowMinutes));
        }

        private static string BuildPublicAuthPartitionKey(HttpContext httpContext) =>
            $"public-auth-ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownIpPartition}";

        /// <summary>
        /// Configura rate limiting para los endpoints públicos de auth y registro que revelan si
        /// una cuenta existe: recover-password, activate-password-link, complete-initial-password
        /// y evaluate-document.
        /// </summary>
        /// <remarks>
        /// Bucket ÚNICO compartido por los cuatro endpoints, a propósito: son variantes del mismo
        /// oráculo de enumeración, así que un atacante no debe poder esquivar el límite rotando
        /// entre ellos.
        ///
        /// Backend Redis (igual que el login, no como las políticas in-memory): el estado se
        /// comparte entre instancias de la API y sobrevive a los deploys. Un límite in-memory se
        /// resetearía en cada deploy y se multiplicaría por la cantidad de réplicas, o sea que no
        /// frenaría la enumeración.
        ///
        /// Partición por IP: una red NAT'eada comparte bucket. El default de 30 cada 15 minutos es
        /// holgado para uso real (el wizard hace 1-2 llamadas por persona) y es el mismo tradeoff
        /// que ya acepta el login, que es 10 cada 15 minutos por IP.
        ///
        /// Configurable vía "Authentication:PublicAuth:RateLimitPerWindow" y
        /// "Authentication:PublicAuth:RateLimitWindowMinutes".
        ///
        /// ⚠️ Hereda el fail-open de RedisRateLimiterService: con Redis caído, estos endpoints
        /// quedan sin freno (mismo riesgo que ya corre el login hoy).
        /// </remarks>
        public static IServiceCollection AddPublicAuthRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var (limit, window) = GetPublicAuthRateLimit(configuration);

            services.AddRateLimiter(options =>
            {
                options.AddPolicy(PublicAuthRateLimitPolicy, httpContext =>
                {
                    // Endpoints siempre anónimos: no hay usuario, particionamos por IP.
                    var redisService = httpContext.RequestServices.GetRequiredService<IRateLimiterService>();

                    return RateLimitPartition.Get(
                        BuildPublicAuthPartitionKey(httpContext),
                        key => new RedisRateLimiter(redisService, key, limit, window));
                });

                // OnRejected NO se asigna acá: el dispatch único vive en AddLoginRateLimiting.
            });

            return services;
        }

        private static async Task HandlePublicAuthRejected(
            OnRejectedContext context, CancellationToken cancellationToken)
        {
            PublicAuthRateLimitRejections.Inc();

            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var (limit, window) = GetPublicAuthRateLimit(configuration);

            var redisService = context.HttpContext.RequestServices.GetRequiredService<IRateLimiterService>();
            var partitionKey = BuildPublicAuthPartitionKey(context.HttpContext);

            var remaining = await redisService.GetRemainingAsync(partitionKey, limit, window);
            var resetTime = await redisService.GetResetTimeAsync(partitionKey, window);

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

            if (resetTime.HasValue)
            {
                context.HttpContext.Response.Headers["X-RateLimit-Reset"] =
                    resetTime.Value.ToUnixTimeSeconds().ToString();
                context.HttpContext.Response.Headers.RetryAfter =
                    Math.Max(0, (int)(resetTime.Value - DateTimeOffset.UtcNow).TotalSeconds).ToString();
            }

            // Mensaje genérico: no dice qué endpoint ni por qué, para no dar pistas al que enumera.
            var result = OperationResult<object>.IsFailed(
                "AUTH_RL_05",
                PublicAuthRateLimitPolicy,
                $"Se superó el límite de solicitudes desde esta red. Intentá nuevamente en {(int)window.TotalMinutes} minutos.",
                429,
                default!);

            await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken);
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
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownIpPartition;
                    var partitionKey = $"login-ip:{ipAddress}";

                    // Usar Redis Rate Limiter en lugar de in-memory
                    var redisService = httpContext.RequestServices.GetRequiredService<AppLogic.Platform.RateLimiting.IRateLimiterService>();

                    return RateLimitPartition.Get(
                        partitionKey,
                        key => new RedisRateLimiter(
                            redisService,
                            key,
                            maxIpAttempts,  // 10 intentos (múltiples usuarios en misma IP)
                            TimeSpan.FromMinutes(windowMinutes)));
                });

                // Único punto donde se asigna OnRejected para todas las políticas de rate
                // limiting: RateLimiterOptions es compartido entre AddRateLimiter
                // calls, así que despachamos acá por política en vez de dejar que la última
                // registrada pise a las demás.
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var policyName = context.HttpContext.GetEndpoint()?.Metadata
                        .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

                    if (policyName == ReconocimientoDocumentoRateLimitPolicy)
                    {
                        await HandleDocumentRecognitionRejected(context, cancellationToken);
                        return;
                    }

                    if (policyName == PhoneValidationRateLimitPolicy)
                    {
                        await HandlePhoneValidationRejected(context, cancellationToken);
                        return;
                    }

                    if (policyName == PublicAuthRateLimitPolicy)
                    {
                        await HandlePublicAuthRejected(context, cancellationToken);
                        return;
                    }

                    LoginRateLimitRejections.Inc();

                    // Obtener información adicional desde Redis
                    var redisService = context.HttpContext.RequestServices.GetRequiredService<AppLogic.Platform.RateLimiting.IRateLimiterService>();
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownIpPartition;
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
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)(resetTime.Value - DateTimeOffset.UtcNow).TotalSeconds).ToString();
                    }

                    var result = OperationResult<AuthenticationResponse>.IsFailed(
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
