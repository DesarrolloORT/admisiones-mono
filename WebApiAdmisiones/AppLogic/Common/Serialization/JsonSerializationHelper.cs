using System.Text.Json;

namespace AppLogic.Common.Serialization;

/// <summary>
/// Deserialización segura: null/vacío o JSON inválido devuelven <c>default</c> en vez de propagar la excepción.
/// </summary>
public static class JsonSerializationHelper
{
    public static T? TryDeserialize<T>(string? json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
