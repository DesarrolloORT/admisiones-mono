namespace WebApiAdmisiones.Security.interfaces
{
    /// <summary>
    /// Interfaz para el servicio de cache distribuido con Redis.
    /// </summary>
    public interface IRedisCacheService
    {
        /// <summary>
        /// Obtiene un valor desde cache. Si no existe, ejecuta la función factory, guarda el resultado y lo devuelve.
        /// </summary>
        Task<T?> GetOrSetAsync<T>(
            string cacheKey,
            Func<Task<T?>> factory,
            TimeSpan ttl) where T : class;

        /// <summary>
        /// Invalida (elimina) una clave del cache.
        /// </summary>
        Task<bool> InvalidateAsync(string cacheKey);

        /// <summary>
        /// Obtiene información sobre el tiempo restante de expiración de una clave.
        /// </summary>
        Task<TimeSpan?> GetTtlAsync(string cacheKey);
    }
}
