using AppLogic.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Utilities;

namespace AppLogic.Utilities
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
