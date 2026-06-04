using StackExchange.Redis;
using System.Text.Json;
using WebApiAdmisiones.Security.interfaces;

namespace WebApiAdmisiones.Security
{
    /// <summary>
    /// Servicio genérico de cache distribuido usando Redis.
    /// Soporta serialización JSON y TTL configurable por clave.
    /// </summary>
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(
            IConnectionMultiplexer redis,
            ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _logger = logger;

            // Configuración de serialización JSON (optimizada para DTOs)
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false, // Compacto para Redis
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Obtiene un valor desde cache. Si no existe, ejecuta la función factory, guarda el resultado y lo devuelve.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a cachear</typeparam>
        /// <param name="cacheKey">Clave única en Redis</param>
        /// <param name="factory">Función que genera el valor si no está en cache</param>
        /// <param name="ttl">Tiempo de expiración (Time To Live)</param>
        /// <returns>Valor desde cache o desde la función factory</returns>
        public async Task<T?> GetOrSetAsync<T>(
            string cacheKey,
            Func<Task<T>> factory,
            TimeSpan ttl) where T : class
        {
            try
            {
                var db = _redis.GetDatabase();
                var cachedValue = await db.StringGetAsync(cacheKey);

                // Cache HIT
                if (cachedValue.HasValue)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("🔥 Cache HIT: {CacheKey}", cacheKey);
                    }

                    var stringValue = cachedValue.ToString();
                    return JsonSerializer.Deserialize<T>(stringValue, _jsonOptions);
                }

                // Cache MISS - Ejecutar factory
                _logger.LogInformation("❄️  Cache MISS: {CacheKey}. Fetching from source...", cacheKey);

                var value = await factory();

                if (value != null)
                {
                    // Guardar en cache
                    var serialized = JsonSerializer.Serialize(value, _jsonOptions);
                    await db.StringSetAsync(cacheKey, serialized, ttl);

                    _logger.LogInformation(
                        "💾 Cached: {CacheKey} (TTL: {TTL}, Size: {Size} bytes)",
                        cacheKey,
                        ttl,
                        serialized.Length);
                }

                return value;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex,
                    "⚠️  Redis connection error for key {CacheKey}. Falling back to direct execution.",
                    cacheKey);

                // Fallback: ejecutar factory sin cache
                return await factory();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "⚠️  JSON serialization error for key {CacheKey}. Falling back to direct execution.",
                    cacheKey);

                // Fallback: ejecutar factory sin cache
                return await factory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "⚠️  Unexpected error accessing cache for key {CacheKey}. Falling back to direct execution.",
                    cacheKey);

                // Fallback: ejecutar factory sin cache
                return await factory();
            }
        }

        /// <summary>
        /// Invalida (elimina) una clave del cache.
        /// Útil cuando los datos cambian y necesitás forzar refresh.
        /// </summary>
        /// <param name="cacheKey">Clave a eliminar</param>
        /// <returns>True si se eliminó, False si no existía</returns>
        public async Task<bool> InvalidateAsync(string cacheKey)
        {
            try
            {
                var db = _redis.GetDatabase();
                var deleted = await db.KeyDeleteAsync(cacheKey);

                if (deleted)
                {
                    _logger.LogInformation("🗑️  Cache invalidated: {CacheKey}", cacheKey);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache key {CacheKey}", cacheKey);
                return false;
            }
        }

        /// <summary>
        /// Obtiene información sobre el tiempo restante de expiración de una clave.
        /// </summary>
        public async Task<TimeSpan?> GetTtlAsync(string cacheKey)
        {
            try
            {
                var db = _redis.GetDatabase();
                return await db.KeyTimeToLiveAsync(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TTL for key {CacheKey}", cacheKey);
                return null;
            }
        }
    }
}
