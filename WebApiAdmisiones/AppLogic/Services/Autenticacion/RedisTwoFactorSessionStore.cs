using AppLogic.DTOs;
using AppLogic.IServices.Autenticacion;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace AppLogic.Services.Autenticacion;

/// <summary>
/// Almacena sesiones 2FA en Redis. Maneja la serialización JSON y el TTL de las claves.
/// Redis key pattern: 2fa:session:{sessionId}
/// </summary>
public class RedisTwoFactorSessionStore : ITwoFactorSessionStore
{
    private const string KeyPrefix = "2fa:session:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IDatabase _db;
    private readonly ILogger<RedisTwoFactorSessionStore> _logger;

    public RedisTwoFactorSessionStore(
        IConnectionMultiplexer redis,
        ILogger<RedisTwoFactorSessionStore> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public Task SaveAsync(string sessionId, TwoFactorSession session, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(session, JsonOptions);
        return _db.StringSetAsync($"{KeyPrefix}{sessionId}", json, ttl);
    }

    public async Task<TwoFactorSession?> GetAsync(string sessionId)
    {
        var key = $"{KeyPrefix}{sessionId}";
        var value = await _db.StringGetAsync(key);

        if (!value.HasValue)
            return null;

        try
        {
            var session = JsonSerializer.Deserialize<TwoFactorSession>(value.ToString(), JsonOptions);
            if (session != null)
                return session;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error al deserializar la sesión 2FA {SessionId}", sessionId);
        }

        // Corrupted session: eliminar para no dejarla en Redis
        await _db.KeyDeleteAsync(key);
        return null;
    }

    public Task<TimeSpan?> GetTtlAsync(string sessionId)
        => _db.KeyTimeToLiveAsync($"{KeyPrefix}{sessionId}");

    public Task UpdateAsync(string sessionId, TwoFactorSession session, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(session, JsonOptions);
        return _db.StringSetAsync($"{KeyPrefix}{sessionId}", json, ttl);
    }

    public Task DeleteAsync(string sessionId)
        => _db.KeyDeleteAsync($"{KeyPrefix}{sessionId}");
}
