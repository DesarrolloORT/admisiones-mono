using System;
using System.Globalization;
using System.Linq;
using Utilities;

namespace AppLogic.Common.Validation
{
    public static class DocumentUtils
    {
        public enum DocumentValidationError
        {
            None,
            InvalidDocumentType,
            InvalidDocument
        }

        public readonly record struct DocumentValidationResult(
            bool IsValid,
            DocumentValidationError Error,
            string Message);

        private static readonly string[] TiposDocumentoPermitidos = ["CI", "DE", "PS", "CC"];

        public static DocumentValidationResult ValidarDocumentoBase(string? tipoDocumentoRaw, string? documentoRaw)
        {
            var tipoDocumento = Normalizar(tipoDocumentoRaw);
            var documento = Normalizar(documentoRaw);

            if (!TiposDocumentoPermitidos.Contains(tipoDocumento))
            {
                return new DocumentValidationResult(false, DocumentValidationError.InvalidDocumentType, "Tipo de documento inválido.");
            }

            if (tipoDocumento == "CI")
            {
                var mensaje = Util.ValidoCI(documento);
                if (!string.IsNullOrWhiteSpace(mensaje))
                {
                    return new DocumentValidationResult(false, DocumentValidationError.InvalidDocument, mensaje);
                }
            }

            return new DocumentValidationResult(true, DocumentValidationError.None, string.Empty);
        }

        public static string Normalizar(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        public static string? NormalizarOpcional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public static string NormalizarMayusculas(string? value)
        {
            return Util.RemplazarTildes(Normalizar(value)).ToUpperInvariant();
        }

        public static string? NormalizarSiNo(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
        }

        public static bool EsSi(string? value)
        {
            return string.Equals(NormalizarSiNo(value), "SI", StringComparison.Ordinal);
        }

        public static string NormalizarDocumentoParaClave(string? value)
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

        public static string NormalizarTipoDocumento(string? tipoDocumento)
        {
            return NormalizarMayusculas(tipoDocumento);
        }

        public static string NormalizarDocumentoIdentidad(string? tipoDocumento, string? documento)
        {
            var tipoNormalizado = NormalizarTipoDocumento(tipoDocumento);
            var documentoNormalizado = NormalizarMayusculas(documento);
            return string.Equals(tipoNormalizado, "CI", StringComparison.Ordinal)
                ? new string(documentoNormalizado.Where(char.IsDigit).ToArray())
                : documentoNormalizado;
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

        public static string FormatearTextoCapitalizado(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var text = string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower(CultureInfo.CurrentCulture));
        }

        public static string? FormatearTextoCapitalizadoOpcional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : FormatearTextoCapitalizado(value);
        }
    }
}
