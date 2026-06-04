using AppLogic.IServices;
using StackExchange.Redis;

namespace AppLogic.Services;

/// <summary>
/// Almacena hashes de tokens de activación en Redis.
/// Reemplaza la columna t_persona.HashTokenPassword.
/// Redis key: registro:hash-token:{key}
/// </summary>
public class RedisHashTokenStore : IHashTokenStore
{
    private const string KeyPrefix = "registro:hash-token:";
    private readonly IDatabase _db;

    public RedisHashTokenStore(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public Task StoreAsync(string key, string hash, TimeSpan ttl)
        => _db.StringSetAsync($"{KeyPrefix}{key}", hash, ttl);

    public async Task<string?> GetAsync(string key)
    {
        var value = await _db.StringGetAsync($"{KeyPrefix}{key}");
        return value.HasValue ? value.ToString() : null;
    }

    public Task DeleteAsync(string key)
        => _db.KeyDeleteAsync($"{KeyPrefix}{key}");
}
