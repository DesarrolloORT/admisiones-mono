using System.Globalization;
using Utilities;

namespace AppLogic.Helpers
{
    public static class RegistroNormalizationHelper
    {
        public static string Normalizar(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        public static string NormalizarMayusculas(string? value)
        {
            return Util.RemplazarTildes(Normalizar(value)).ToUpperInvariant();
        }

        public static string FormatoCapital(string? value)
        {
            var normalized = Normalizar(value).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
        }
    }
}
