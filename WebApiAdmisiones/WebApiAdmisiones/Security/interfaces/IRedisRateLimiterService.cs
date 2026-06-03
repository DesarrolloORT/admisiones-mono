namespace WebApiAdmisiones.Security.interfaces
{
    /// <summary>
    /// Interfaz para el servicio de rate limiting distribuido con Redis.
    /// </summary>
    public interface IRedisRateLimiterService
    {
        /// <summary>
        /// Verifica si una solicitud está permitida según el rate limit.
        /// </summary>
        Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);

        /// <summary>
        /// Obtiene el número de intentos restantes para una clave.
        /// </summary>
        Task<int> GetRemainingAsync(string key, int limit, TimeSpan window);

        /// <summary>
        /// Obtiene el momento en que se restablecerá el rate limit para una clave.
        /// </summary>
        Task<DateTimeOffset?> GetResetTimeAsync(string key, TimeSpan window);

        /// <summary>
        /// Limpia el contador de rate limit para una clave específica.
        /// </summary>
        Task<bool> ClearAsync(string key);

        /// <summary>
        /// Valida un intento de login con rate limiting por cuenta (IP + Documento).
        /// </summary>
        Task<RateLimitValidationResult> ValidateAsync(
            string ipAddress,
            string? tipoDocumento,
            string? documento,
            int limit,
            TimeSpan window);
    }
}
