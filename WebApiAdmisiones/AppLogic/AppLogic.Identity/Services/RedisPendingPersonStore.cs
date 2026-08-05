using AppLogic.Contracts.Text;
using AppLogic.Identity.Dtos;
using System.Text.Json;
using AppLogic.Identity.Interfaces;
using AppLogic.Platform.Serialization;
using AppLogic.Identity;
using StackExchange.Redis;

namespace AppLogic.Identity.Services;

public sealed class RedisPendingPersonStore : IPendingPersonStore
{
    private const string PendingPersonaKeyPrefix = "registro:pending:";
    private const string PendingPersonaDocumentoKeyPrefix = "registro:pending-doc:";

    private readonly IDatabase _redisDb;

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializationDefaults.Redis;

    public RedisPendingPersonStore(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    public async Task SaveAsync(PendingPerson pending, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(pending, JsonOptions);
        await _redisDb.StringSetAsync($"{PendingPersonaKeyPrefix}{pending.FlowId}", json, ttl);
        await _redisDb.StringSetAsync(
            BuildPendingDocumentKey(pending.DocumentType, pending.DocumentNumber),
            pending.FlowId,
            ttl);
    }

    public async Task<string?> GetRawAsync(string flowId)
    {
        var json = await _redisDb.StringGetAsync($"{PendingPersonaKeyPrefix}{flowId}");
        return json.HasValue ? json.ToString() : null;
    }

    public async Task<PendingPerson?> GetAsync(string flowId)
    {
        var json = await GetRawAsync(flowId);
        if (json == null) return null;

        return JsonSerialization.TryDeserialize<PendingPerson>(json, JsonOptions);
    }

    public async Task DeleteAsync(string flowId)
    {
        var pending = await GetAsync(flowId);
        if (pending != null)
        {
            await _redisDb.KeyDeleteAsync(BuildPendingDocumentKey(pending.DocumentType, pending.DocumentNumber));
        }

        await _redisDb.KeyDeleteAsync($"{PendingPersonaKeyPrefix}{flowId}");
    }

    public async Task<string?> ResolveFlowIdByDocumentAsync(string documentType, string document)
    {
        var docKey = BuildPendingDocumentKey(documentType, document);
        var existingFlowId = await _redisDb.StringGetAsync(docKey);
        if (!existingFlowId.HasValue)
        {
            return null;
        }

        var flowId = existingFlowId.ToString();
        if (string.IsNullOrWhiteSpace(flowId))
        {
            await _redisDb.KeyDeleteAsync(docKey);
            return null;
        }

        var pending = await GetAsync(flowId);
        if (pending != null)
        {
            return flowId;
        }

        await _redisDb.KeyDeleteAsync(docKey);
        return null;
    }

    private static string BuildPendingDocumentKey(string documentType, string document)
    {
        var tipoNormalizado = TextNormalization.ToUpperWithoutAccents(documentType);
        var normalizedDocument = TextNormalization.ToUpperWithoutAccents(document);
        if (IdentityDocumentRules.IsNationalId(tipoNormalizado))
        {
            normalizedDocument = new string(normalizedDocument.Where(char.IsDigit).ToArray());
        }

        return $"{PendingPersonaDocumentoKeyPrefix}{tipoNormalizado}:{normalizedDocument}";
    }
}
