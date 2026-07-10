using AppLogic.Registro.Dtos;
using System.Text.Json;
using AppLogic.Registro.Interfaces;
using AppLogic.Common.Serialization;
using AppLogic.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AppLogic.Registro.Services;

public sealed class RegistroDocumentoImagenCacheService : IRegistroDocumentoImagenCacheService
{
    private const string KeyPrefix = "registro:documento-imagenes:";
    private const string TtlConfigKey = "Registro:DocumentoImagenesTTLHours";
    private const double DefaultTtlHours = 24;

    private readonly IConfiguration _configuration;
    private readonly IDatabase _redisDb;
    private readonly ILogger<RegistroDocumentoImagenCacheService>? _logger;

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializationDefaults.Redis;

    public RegistroDocumentoImagenCacheService(
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        ILogger<RegistroDocumentoImagenCacheService>? logger = null)
    {
        _configuration = configuration;
        _redisDb = redis.GetDatabase();
        _logger = logger;
    }

    public async Task GuardarAsync(
        string tipoDocumento,
        string documento,
        DtoRegistroDocumentoImagenesTemporales imagenes)
    {
        var key = CrearKey(tipoDocumento, documento);
        var ttlHours = _configuration.GetValue<double?>(TtlConfigKey) ?? DefaultTtlHours;
        var ttl = TimeSpan.FromHours(ttlHours > 0 ? ttlHours : DefaultTtlHours);

        imagenes.TipoDocumento = DocumentUtils.NormalizarTipoDocumento(tipoDocumento);
        imagenes.Documento = DocumentUtils.NormalizarDocumentoIdentidad(imagenes.TipoDocumento, documento);
        imagenes.CreatedAt = imagenes.CreatedAt == default ? DateTime.UtcNow : imagenes.CreatedAt;

        var json = JsonSerializer.Serialize(imagenes, JsonOptions);
        await _redisDb.StringSetAsync(key, json, ttl);
    }

    public async Task GuardarImagenesTemporalesSiCorrespondeAsync(
        string? tipoDocumento,
        string? numeroDocumento,
        DateTime? fechaVencimiento,
        DtoRegistroDocumentoArchivoTemporal documentoFrente,
        DtoRegistroDocumentoArchivoTemporal? caraPersona)
    {
        if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(numeroDocumento))
        {
            return;
        }

        try
        {
            await GuardarAsync(
                tipoDocumento,
                numeroDocumento,
                new DtoRegistroDocumentoImagenesTemporales
                {
                    TipoDocumento = tipoDocumento,
                    Documento = numeroDocumento,
                    FechaVencimiento = fechaVencimiento,
                    DocumentoFrente = documentoFrente,
                    CaraPersona = caraPersona,
                    CreatedAt = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "No se pudieron guardar en Redis las imagenes reconocidas para {TipoDocumento}:{Documento}.",
                tipoDocumento,
                numeroDocumento);
        }
    }

    public async Task<DtoRegistroDocumentoImagenesTemporales?> ObtenerAsync(string tipoDocumento, string documento)
    {
        var json = await _redisDb.StringGetAsync(CrearKey(tipoDocumento, documento));
        if (!json.HasValue)
        {
            return null;
        }

        return JsonSerializationHelper.TryDeserialize<DtoRegistroDocumentoImagenesTemporales>(
            json.ToString(),
            JsonOptions);
    }

    public Task EliminarAsync(string tipoDocumento, string documento)
        => _redisDb.KeyDeleteAsync(CrearKey(tipoDocumento, documento));

    internal static string CrearKey(string tipoDocumento, string documento)
    {
        var tipoNormalizado = DocumentUtils.NormalizarTipoDocumento(tipoDocumento);
        var documentoNormalizado = DocumentUtils.NormalizarDocumentoIdentidad(tipoNormalizado, documento);
        return $"{KeyPrefix}{tipoNormalizado}:{documentoNormalizado}";
    }
}
