using StackExchange.Redis;

namespace WebApiAdmisiones.Security.RateLimiting
{
    /// <summary>
    /// Rate limiter distribuido usando Redis.
    /// Permite compartir estado de rate limiting entre múltiples instancias de la API.
    /// Implementa Sliding Window algorithm para mayor precisión.
    /// </summary>
    public class RedisRateLimiterService : IRedisRateLimiterService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisRateLimiterService> _logger;

        public RedisRateLimiterService(
            IConnectionMultiplexer redis,
            ILogger<RedisRateLimiterService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        /// <summary>
        /// Implementa Sliding Window Rate Limiting usando Redis Sorted Sets.
        /// </summary>
        /// <param name="key">Clave única de partición (ej: "login-ip:192.168.1.1")</param>
        /// <param name="limit">Máximo número de requests permitidos</param>
        /// <param name="window">Ventana de tiempo</param>
        /// <returns>True si está permitido, False si excede el límite</returns>
        /// <remarks>
        /// Algoritmo:
        /// 1. Usa Sorted Set en Redis con timestamp como score
        /// 2. Elimina requests fuera de la ventana de tiempo
        /// 3. Agrega el request actual
        /// 4. Cuenta total de requests en la ventana
        /// 5. Permite si count <= limit
        /// 
        /// Ventajas sobre Fixed Window:
        /// - No hay "burst" al inicio de cada ventana
        /// - Más preciso: cada request expira exactamente después de la ventana
        /// </remarks>
        public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
        {
            try
            {
                var db = _redis.GetDatabase();
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var windowMs = (long)window.TotalMilliseconds;
                var windowStart = now - windowMs;

                var redisKey = $"ratelimit:{key}";

                // Transacción atómica en Redis para consistencia
                var transaction = db.CreateTransaction();

                // 1. Eliminar requests antiguos fuera de la ventana (cleanup)
                var removeTask = transaction.SortedSetRemoveRangeByScoreAsync(
                    redisKey,
                    double.NegativeInfinity,
                    windowStart);

                // 2. Agregar el request actual con timestamp como score
                var addTask = transaction.SortedSetAddAsync(redisKey, now, now);

                // 3. Contar requests dentro de la ventana
                var countTask = transaction.SortedSetLengthAsync(redisKey);

                // 4. Establecer expiración para auto-limpieza de claves viejas
                var expireTask = transaction.KeyExpireAsync(redisKey, window.Add(TimeSpan.FromMinutes(1)));

                // Ejecutar todas las operaciones atómicamente
                var executed = await transaction.ExecuteAsync();

                if (!executed)
                {
                    _logger.LogWarning(
                        "Redis transaction failed for rate limit key {Key}. Failing safe (rejecting request).",
                        redisKey);
                    return false; // Fail-safe: rechazar request si Redis falla
                }

                var count = await countTask;
                var allowed = count <= limit;

                if (!allowed)
                {
                    _logger.LogWarning(
                        "Rate limit EXCEEDED for {Key}. Current: {Count}/{Limit} in window {Window}",
                        key, count, limit, window);
                }
                else if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Rate limit check OK for {Key}. Current: {Count}/{Limit}",
                        key, count, limit);
                }

                return allowed;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, 
                    "Redis connection error checking rate limit for key {Key}. Failing open (allowing request).", 
                    key);
                // Fail-open: permitir request si Redis no está disponible (para no bloquear la app)
                // Cambiar a 'return false' si preferís fail-closed (más seguro pero afecta disponibilidad)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Unexpected error checking rate limit for key {Key}. Failing open (allowing request).", 
                    key);
                return true;
            }
        }

        /// <summary>
        /// Obtiene el número de requests restantes disponibles en la ventana actual.
        /// Útil para devolver headers X-RateLimit-Remaining.
        /// </summary>
        /// <param name="key">Clave de partición</param>
        /// <param name="limit">Límite máximo configurado</param>
        /// <param name="window">Ventana de tiempo</param>
        /// <returns>Requests restantes (0 si excedido)</returns>
        public async Task<int> GetRemainingAsync(string key, int limit, TimeSpan window)
        {
            try
            {
                var db = _redis.GetDatabase();
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var windowStart = now - (long)window.TotalMilliseconds;
                var redisKey = $"ratelimit:{key}";

                var count = await db.SortedSetLengthAsync(redisKey, windowStart, now);
                return Math.Max(0, limit - (int)count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting remaining rate limit for key {Key}", key);
                return 0; // Retornar 0 si hay error (conservador)
            }
        }

        /// <summary>
        /// Obtiene el timestamp de reset (cuando expira el request más antiguo en la ventana).
        /// Útil para header X-RateLimit-Reset.
        /// </summary>
        public async Task<DateTimeOffset?> GetResetTimeAsync(string key, TimeSpan window)
        {
            try
            {
                var db = _redis.GetDatabase();
                var redisKey = $"ratelimit:{key}";

                // Obtener el request más antiguo (primer elemento del sorted set)
                var oldest = await db.SortedSetRangeByScoreAsync(redisKey, take: 1);

                if (oldest.Length == 0)
                    return null;

                var oldestTimestamp = (long)(double)oldest[0];
                var resetTime = DateTimeOffset.FromUnixTimeMilliseconds(oldestTimestamp).Add(window);

                return resetTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reset time for key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Limpia manualmente el estado de rate limiting para una clave específica.
        /// Útil para testing o para resetear manualmente un bloqueo.
        /// </summary>
        public async Task<bool> ClearAsync(string key)
        {
            try
            {
                var db = _redis.GetDatabase();
                var redisKey = $"ratelimit:{key}";
                var deleted = await db.KeyDeleteAsync(redisKey);

                if (deleted)
                {
                    _logger.LogInformation("Rate limit cleared for key {Key}", key);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing rate limit for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Valida rate limiting para una combinación IP + Documento específica.
        /// Este método debe llamarse DESPUÉS de validar el rate limit por IP.
        /// </summary>
        /// <param name="ipAddress">Dirección IP del cliente</param>
        /// <param name="tipoDocumento">Tipo de documento (ej: "CI", "Pasaporte")</param>
        /// <param name="documento">Número de documento</param>
        /// <param name="limit">Límite de intentos permitidos</param>
        /// <param name="window">Ventana de tiempo</param>
        /// <returns>Información sobre si está permitido y cuántos intentos quedan</returns>
        /// <remarks>
        /// Protección dual OWASP Anti-Automation:
        /// - Previene ataques distribuidos a una cuenta específica desde múltiples IPs
        /// - Complementa el rate limit por IP (que previene probar múltiples cuentas desde una IP)
        /// 
        /// Ejemplo de uso en Login:
        /// 1. Middleware valida rate limit por IP (antes de leer el body)
        /// 2. Controller valida rate limit por IP+Documento (después de deserializar el request)
        /// 3. Si cualquiera de los dos falla → HTTP 429
        /// </remarks>
        public async Task<RateLimitValidationResult> ValidateAsync(
            string ipAddress, 
            string tipoDocumento, 
            string documento, 
            int limit, 
            TimeSpan window)
        {
            var normalizedDoc = NormalizeDocumento(documento);
            var key = $"login-account:{tipoDocumento}:{normalizedDoc}:ip:{ipAddress}";

            var allowed = await IsAllowedAsync(key, limit, window);
            var remaining = await GetRemainingAsync(key, limit, window);
            var resetTime = await GetResetTimeAsync(key, window);

            return new RateLimitValidationResult
            {
                IsAllowed = allowed,
                RemainingAttempts = remaining,
                ResetTime = resetTime,
                PartitionKey = key
            };
        }

        /// <summary>
        /// Normaliza el número de documento para evitar bypass por formato
        /// (ej: "1.234.567-8" vs "12345678").
        /// </summary>
        private static string NormalizeDocumento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return "unknown";

            // Remover puntos, guiones, espacios
            return documento.Replace(".", "")
                           .Replace("-", "")
                           .Replace(" ", "")
                           .Trim()
                           .ToLowerInvariant();
        }
    }

    /// <summary>
    /// Resultado de validación de rate limiting.
    /// </summary>
    public class RateLimitValidationResult
    {
        /// <summary>
        /// Indica si el request está permitido.
        /// </summary>
        public bool IsAllowed { get; init; }

        /// <summary>
        /// Número de intentos restantes en la ventana actual.
        /// </summary>
        public int RemainingAttempts { get; init; }

        /// <summary>
        /// Timestamp de cuando se resetea el contador (UTC).
        /// </summary>
        public DateTimeOffset? ResetTime { get; init; }

        /// <summary>
        /// Clave de partición utilizada (para logging/debugging).
        /// </summary>
        public required string PartitionKey { get; init; }
    }
}
