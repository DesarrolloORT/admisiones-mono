namespace AppLogic.Authentication.Interfaces;

/// <summary>
/// Almacenamiento de hashes de tokens de activacion.
/// </summary>
public interface IHashTokenStore
{
    /// <summary>Almacena el hash con el TTL indicado.</summary>
    Task StoreAsync(string key, string hash, TimeSpan ttl);

    /// <summary>Devuelve el hash almacenado, o null si no existe o expiro.</summary>
    Task<string?> GetAsync(string key);

    /// <summary>Elimina la entrada.</summary>
    Task DeleteAsync(string key);
}
