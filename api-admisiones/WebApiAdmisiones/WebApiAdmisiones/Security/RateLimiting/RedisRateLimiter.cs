using AppLogic.Platform.RateLimiting;
using System.Threading.RateLimiting;

namespace WebApiAdmisiones.Security.RateLimiting
{
    /// <summary>
    /// Implementación de RateLimiter que usa Redis como backend.
    /// Se integra con el sistema de rate limiting de ASP.NET Core.
    /// </summary>
    public sealed class RedisRateLimiter : RateLimiter
    {
        private readonly IRateLimiterService _redisService;
        private readonly string _key;
        private readonly int _limit;
        private readonly TimeSpan _window;

        public RedisRateLimiter(
            IRateLimiterService redisService,
            string key,
            int limit,
            TimeSpan window)
        {
            _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _limit = limit;
            _window = window;
        }

        /// <summary>
        /// Tiempo de inactividad (no aplicable para Redis, siempre null).
        /// </summary>
        public override TimeSpan? IdleDuration => null;

        /// <summary>
        /// Obtiene estadísticas del rate limiter (nuevo en .NET 10).
        /// </summary>
        public override RateLimiterStatistics? GetStatistics()
        {
            // Redis no mantiene estadísticas agregadas en memoria
            // Retornamos null para indicar que no están disponibles
            return null;
        }

        /// <summary>
        /// Versión asíncrona de adquisición de permiso (preferida para Redis).
        /// </summary>
        protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
            int permitCount, 
            CancellationToken cancellationToken)
        {
            if (permitCount != 1)
            {
                // Redis rate limiter solo soporta 1 permit a la vez
                return new RedisRateLimitLease(isAcquired: false);
            }

            var allowed = await _redisService.IsAllowedAsync(_key, _limit, _window);
            return new RedisRateLimitLease(isAcquired: allowed);
        }

        /// <summary>
        /// Versión síncrona (no recomendada con Redis, pero requerida por la interfaz).
        /// Se implementa como wrapper de la versión async.
        /// </summary>
        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            // IMPORTANTE: Llamar sync sobre async puede causar deadlocks
            // Pero es requerido por la interfaz RateLimiter
            // El middleware de ASP.NET Core usa AcquireAsyncCore, así que esto raramente se llama
            if (permitCount != 1)
            {
                return new RedisRateLimitLease(isAcquired: false);
            }

            var allowed = _redisService.IsAllowedAsync(_key, _limit, _window)
                .GetAwaiter()
                .GetResult();

            return new RedisRateLimitLease(isAcquired: allowed);
        }

        /// <summary>
        /// Representación del "lease" o permiso de rate limiting.
        /// </summary>
        private sealed class RedisRateLimitLease : RateLimitLease
        {
            private readonly bool _isAcquired;

            public RedisRateLimitLease(bool isAcquired)
            {
                _isAcquired = isAcquired;
            }

            /// <summary>
            /// Indica si el permiso fue concedido.
            /// </summary>
            public override bool IsAcquired => _isAcquired;

            /// <summary>
            /// Metadata disponible (ninguna en esta implementación).
            /// </summary>
            public override IEnumerable<string> MetadataNames => Array.Empty<string>();

            /// <summary>
            /// Intenta obtener metadata (no soportado).
            /// </summary>
            public override bool TryGetMetadata(string metadataName, out object? metadata)
            {
                metadata = null;
                return false;
            }

            /// <summary>
            /// Libera recursos (no necesario para Redis).
            /// </summary>
            protected override void Dispose(bool disposing)
            {
                // No hay recursos para liberar en Redis
            }
        }

        /// <summary>
        /// Libera recursos del rate limiter (no necesario para Redis).
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            // RedisRateLimiterService es singleton, no lo disponemos aquí
        }

        /// <summary>
        /// Versión asíncrona de Dispose.
        /// </summary>
        protected override ValueTask DisposeAsyncCore()
        {
            return ValueTask.CompletedTask;
        }
    }
}
