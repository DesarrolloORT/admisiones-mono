using AppLogic.Registro.Dtos;
using System.Text.Json;
using AppLogic.Registro.Interfaces;
using AppLogic.Common.Serialization;
using AppLogic.Common.Validation;
using StackExchange.Redis;

namespace AppLogic.Registro.Services;

public sealed class PendingPersonaRedisStore : IPendingPersonaStore
{
    private const string PendingPersonaKeyPrefix = "registro:pending:";
    private const string PendingPersonaDocumentoKeyPrefix = "registro:pending-doc:";

    private readonly IDatabase _redisDb;

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializationDefaults.Redis;

    public PendingPersonaRedisStore(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    public async Task SaveAsync(DtoRegistroPendingPersona pending, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(pending, JsonOptions);
        await _redisDb.StringSetAsync($"{PendingPersonaKeyPrefix}{pending.FlowId}", json, ttl);
        await _redisDb.StringSetAsync(
            CrearPendingDocumentoKey(pending.TipoDocumento, pending.Documento),
            pending.FlowId,
            ttl);
    }

    public async Task<string?> GetRawAsync(string flowId)
    {
        var json = await _redisDb.StringGetAsync($"{PendingPersonaKeyPrefix}{flowId}");
        return json.HasValue ? json.ToString() : null;
    }

    public async Task<DtoRegistroPendingPersona?> GetAsync(string flowId)
    {
        var json = await GetRawAsync(flowId);
        if (json == null) return null;

        return JsonSerializationHelper.TryDeserialize<DtoRegistroPendingPersona>(json, JsonOptions);
    }

    public async Task DeleteAsync(string flowId)
    {
        var pending = await GetAsync(flowId);
        if (pending != null)
        {
            await _redisDb.KeyDeleteAsync(CrearPendingDocumentoKey(pending.TipoDocumento, pending.Documento));
        }

        await _redisDb.KeyDeleteAsync($"{PendingPersonaKeyPrefix}{flowId}");
    }

    public async Task<string?> ResolverFlowIdPorDocumentoAsync(string tipoDocumento, string documento)
    {
        var docKey = CrearPendingDocumentoKey(tipoDocumento, documento);
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

    private static string CrearPendingDocumentoKey(string tipoDocumento, string documento)
    {
        var tipoNormalizado = DocumentUtils.NormalizarMayusculas(tipoDocumento);
        var documentoNormalizado = DocumentUtils.NormalizarMayusculas(documento);
        if (DocumentUtils.EsCedula(tipoNormalizado))
        {
            documentoNormalizado = new string(documentoNormalizado.Where(char.IsDigit).ToArray());
        }

        return $"{PendingPersonaDocumentoKeyPrefix}{tipoNormalizado}:{documentoNormalizado}";
    }
}
