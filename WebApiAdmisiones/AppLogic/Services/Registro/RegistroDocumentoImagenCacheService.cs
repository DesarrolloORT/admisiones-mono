using System.Text.Json;
using AppLogic.DTOs;
using AppLogic.IServices.Registro;
using AppLogic.Utilities;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace AppLogic.Services.Registro;

public sealed class RegistroDocumentoImagenCacheService : IRegistroDocumentoImagenCacheService
{
    private const string KeyPrefix = "registro:documento-imagenes:";
    private const string TtlConfigKey = "Registro:DocumentoImagenesTTLHours";
    private const double DefaultTtlHours = 24;

    private readonly IConfiguration _configuration;
    private readonly IDatabase _redisDb;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public RegistroDocumentoImagenCacheService(
        IConfiguration configuration,
        IConnectionMultiplexer redis)
    {
        _configuration = configuration;
        _redisDb = redis.GetDatabase();
    }

    public async Task GuardarAsync(
        string tipoDocumento,
        string documento,
        RegistroDocumentoImagenesTemporales imagenes)
    {
        var key = CrearKey(tipoDocumento, documento);
        var ttlHours = _configuration.GetValue<double?>(TtlConfigKey) ?? DefaultTtlHours;
        var ttl = TimeSpan.FromHours(ttlHours > 0 ? ttlHours : DefaultTtlHours);

        imagenes.TipoDocumento = NormalizarTipoDocumento(tipoDocumento);
        imagenes.Documento = NormalizarDocumento(imagenes.TipoDocumento, documento);
        imagenes.CreatedAt = imagenes.CreatedAt == default ? DateTime.UtcNow : imagenes.CreatedAt;

        var json = JsonSerializer.Serialize(imagenes, JsonOptions);
        await _redisDb.StringSetAsync(key, json, ttl);
    }

    public async Task<RegistroDocumentoImagenesTemporales?> ObtenerAsync(string tipoDocumento, string documento)
    {
        var json = await _redisDb.StringGetAsync(CrearKey(tipoDocumento, documento));
        if (!json.HasValue)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RegistroDocumentoImagenesTemporales>(
                json.ToString(),
                JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public Task EliminarAsync(string tipoDocumento, string documento)
        => _redisDb.KeyDeleteAsync(CrearKey(tipoDocumento, documento));

    internal static string CrearKey(string tipoDocumento, string documento)
    {
        var tipoNormalizado = NormalizarTipoDocumento(tipoDocumento);
        var documentoNormalizado = NormalizarDocumento(tipoNormalizado, documento);
        return $"{KeyPrefix}{tipoNormalizado}:{documentoNormalizado}";
    }

    internal static string NormalizarTipoDocumento(string? tipoDocumento)
        => DocumentUtils.NormalizarMayusculas(tipoDocumento);

    internal static string NormalizarDocumento(string tipoDocumento, string? documento)
    {
        var documentoNormalizado = DocumentUtils.NormalizarMayusculas(documento);
        return string.Equals(tipoDocumento, "CI", StringComparison.Ordinal)
            ? new string(documentoNormalizado.Where(char.IsDigit).ToArray())
            : documentoNormalizado;
    }
}
