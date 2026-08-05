using System;
using System.Linq;
using AppLogic.Contracts.Text;
using Utilities;

namespace AppLogic.Identity;

/// <summary>
/// Reglas del documento de identidad. La normalización de texto sin dominio vive en
/// <see cref="TextNormalization"/>.
/// </summary>
public static class IdentityDocumentRules
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

    public const string TipoDocumentoCedula = "CI";

    private static readonly string[] TiposDocumentoPermitidos = [TipoDocumentoCedula, "DE", "PS", "CC"];

    /// <summary>
    /// Decisión de negocio central del registro: cédula ⇒ flujo persona, otro documento ⇒ solicitud de alta.
    /// </summary>
    public static bool IsNationalId(string? documentType) =>
        string.Equals(TextNormalization.Trim(documentType), TipoDocumentoCedula, StringComparison.Ordinal);

    public static DocumentValidationResult ValidateBaseDocument(string? tipoDocumentoRaw, string? documentoRaw)
    {
        var documentType = TextNormalization.Trim(tipoDocumentoRaw);
        var document = TextNormalization.Trim(documentoRaw);

        if (!TiposDocumentoPermitidos.Contains(documentType))
        {
            return new DocumentValidationResult(false, DocumentValidationError.InvalidDocumentType, "Tipo de documento inválido.");
        }

        if (IsNationalId(documentType))
        {
            var message = Util.ValidoCI(document);
            if (!string.IsNullOrWhiteSpace(message))
            {
                return new DocumentValidationResult(false, DocumentValidationError.InvalidDocument, message);
            }
        }

        return new DocumentValidationResult(true, DocumentValidationError.None, string.Empty);
    }

    public static string NormalizeDocumentType(string? documentType)
    {
        return TextNormalization.ToUpperWithoutAccents(documentType);
    }

    public static string NormalizeIdentityDocument(string? documentType, string? document)
    {
        var tipoNormalizado = NormalizeDocumentType(documentType);
        var normalizedDocument = TextNormalization.ToUpperWithoutAccents(document);
        return IsNationalId(tipoNormalizado)
            ? new string(normalizedDocument.Where(char.IsDigit).ToArray())
            : normalizedDocument;
    }
}
