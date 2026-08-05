using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppLogic.Platform.Serialization;

/// <summary>
/// Opciones de serialización compartidas por los stores que persisten DTOs internos en Redis
/// (camelCase, omitiendo nulls al escribir). No usar donde las opciones actuales difieran
/// intencionalmente (ej. <c>RedisTwoFactorSessionStore</c> no omite nulls).
/// </summary>
public static class JsonSerializationDefaults
{
    public static readonly JsonSerializerOptions Redis = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
