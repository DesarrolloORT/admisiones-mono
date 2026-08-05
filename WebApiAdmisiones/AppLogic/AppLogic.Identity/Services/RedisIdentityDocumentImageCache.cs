using AppLogic.Identity.Dtos;
using System.Text.Json;
using AppLogic.Identity.Interfaces;
using AppLogic.Platform.Serialization;
using AppLogic.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AppLogic.Identity.Services;

public sealed class RedisIdentityDocumentImageCache : IIdentityDocumentImageCache
{
    private const string KeyPrefix = "registro:documento-imagenes:";
    private const string TtlConfigKey = "Registro:DocumentoImagenesTTLHours";
    private const double DefaultTtlHours = 24;

    private readonly IConfiguration _configuration;
    private readonly IDatabase _redisDb;
    private readonly ILogger<RedisIdentityDocumentImageCache>? _logger;

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializationDefaults.Redis;

    public RedisIdentityDocumentImageCache(
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        ILogger<RedisIdentityDocumentImageCache>? logger = null)
    {
        _configuration = configuration;
        _redisDb = redis.GetDatabase();
        _logger = logger;
    }

    public async Task SaveAsync(
        string documentType,
        string document,
        TemporaryDocumentImages images)
    {
        var key = BuildKey(documentType, document);
        var ttlHours = _configuration.GetValue<double?>(TtlConfigKey) ?? DefaultTtlHours;
        var ttl = TimeSpan.FromHours(ttlHours > 0 ? ttlHours : DefaultTtlHours);

        images.DocumentType = IdentityDocumentRules.NormalizeDocumentType(documentType);
        images.DocumentNumber = IdentityDocumentRules.NormalizeIdentityDocument(images.DocumentType, document);
        images.CreatedAt = images.CreatedAt == default ? DateTime.UtcNow : images.CreatedAt;

        var json = JsonSerializer.Serialize(images, JsonOptions);
        await _redisDb.StringSetAsync(key, json, ttl);
    }

    public async Task SaveTemporaryImagesIfApplicableAsync(
        string? documentType,
        string? numeroDocumento,
        DateTime? dueDate,
        TemporaryDocumentFile documentFront,
        TemporaryDocumentFile? personFace)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(numeroDocumento))
        {
            return;
        }

        try
        {
            await SaveAsync(
                documentType,
                numeroDocumento,
                new TemporaryDocumentImages
                {
                    DocumentType = documentType,
                    DocumentNumber = numeroDocumento,
                    ExpirationDate = dueDate,
                    DocumentFront = documentFront,
                    PersonFace = personFace,
                    CreatedAt = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "No se pudieron guardar en Redis las imagenes reconocidas para {TipoDocumento}:{Documento}.",
                documentType,
                numeroDocumento);
        }
    }

    public async Task<TemporaryDocumentImages?> GetAsync(string documentType, string document)
    {
        var json = await _redisDb.StringGetAsync(BuildKey(documentType, document));
        if (!json.HasValue)
        {
            return null;
        }

        return JsonSerialization.TryDeserialize<TemporaryDocumentImages>(
            json.ToString(),
            JsonOptions);
    }

    public Task DeleteAsync(string documentType, string document)
        => _redisDb.KeyDeleteAsync(BuildKey(documentType, document));

    internal static string BuildKey(string documentType, string document)
    {
        var tipoNormalizado = IdentityDocumentRules.NormalizeDocumentType(documentType);
        var normalizedDocument = IdentityDocumentRules.NormalizeIdentityDocument(tipoNormalizado, document);
        return $"{KeyPrefix}{tipoNormalizado}:{normalizedDocument}";
    }
}
