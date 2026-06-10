using System.Diagnostics.CodeAnalysis;

namespace AppLogic.IServices;

public interface IRateLimiterService
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
    Task<int> GetRemainingAsync(string key, int limit, TimeSpan window);
    Task<DateTimeOffset?> GetResetTimeAsync(string key, TimeSpan window);
    Task<bool> ClearAsync(string key);
    Task<RateLimitValidationResult> ValidateAsync(
        string ipAddress,
        string? tipoDocumento,
        string? documento,
        int limit,
        TimeSpan window);
}

[ExcludeFromCodeCoverage]
public class RateLimitValidationResult
{
    public bool IsAllowed { get; init; }
    public int RemainingAttempts { get; init; }
    public DateTimeOffset? ResetTime { get; init; }
    public required string PartitionKey { get; init; }
}
