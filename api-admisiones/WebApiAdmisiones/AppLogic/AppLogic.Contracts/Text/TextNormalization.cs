using System;
using System.Globalization;
using AppLogic.Contracts.Constants;
using Utilities;

namespace AppLogic.Contracts.Text;

/// <summary>
/// Normalización y formato de texto sin conocimiento de dominio. Las reglas propias del documento
/// de identidad (qué es una cédula, qué tipos se aceptan) viven en el módulo Identity, no acá.
/// </summary>
public static class TextNormalization
{
    public static string Trim(string? value) => value?.Trim() ?? string.Empty;

    public static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string ToUpperWithoutAccents(string? value) =>
        Util.RemplazarTildes(Trim(value)).ToUpperInvariant();

    public static string? NormalizeYesNo(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    public static bool IsYes(string? value) =>
        string.Equals(NormalizeYesNo(value), SchemaConstants.BooleanFlag.Yes, StringComparison.Ordinal);

    /// <summary>
    /// Clave estable para particionar por documento (rate limiting): sin puntos, guiones ni espacios.
    /// El llamador y el rate limiter deben normalizar igual, por eso vive en un solo lugar.
    /// </summary>
    public static string NormalizeDocumentForKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        return value.Replace(".", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .Trim()
            .ToLowerInvariant();
    }

    public static string ToTitleCaseInvariant(string? value)
    {
        var normalized = Trim(value).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    public static string ToTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower(CultureInfo.CurrentCulture));
    }

    public static string? ToTitleCaseOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : ToTitleCase(value);
}
