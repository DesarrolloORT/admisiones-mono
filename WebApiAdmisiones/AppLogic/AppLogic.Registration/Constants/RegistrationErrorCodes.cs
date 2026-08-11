namespace AppLogic.Registration.Constants;

/// <summary>
/// Códigos y mensajes que comparten varios casos de uso del registro. Los códigos propios de un
/// solo caso de uso quedan en él.
/// </summary>
public static class RegistrationErrorCodes
{
    /// <summary>Body ausente.</summary>
    public const string MissingRequest = "REG_REQUEST_01";

    public const string MissingRequestMessage = "La solicitud es obligatoria.";

    /// <summary>El tipo de documento recibido no corresponde al caso de uso invocado.</summary>
    public const string UnsupportedDocumentType = "REG_DOC_03";

    /// <summary>Tipo de documento inválido según <c>IdentityDocumentRules</c>.</summary>
    public const string InvalidDocumentType = "REG_DOC_01";

    /// <summary>Resto de los errores de validación del documento.</summary>
    public const string InvalidDocument = "REG_DOC_02";

    /// <summary>El teléfono principal no es un celular válido para su país.</summary>
    public const string InvalidPrimaryPhone = "REG_TEL_01";

    public const string InvalidPrimaryPhoneMessage = "El teléfono principal no es un celular válido.";

    /// <summary>Falta el teléfono principal.</summary>
    public const string MissingPrimaryPhone = "REG_TEL_02";

    public const string MissingPrimaryPhoneMessage = "El teléfono principal es obligatorio.";

    /// <summary>No existe la fila de T_CARACTERISTICA_PAIS para el país del teléfono.</summary>
    public const string UnknownPhoneCountryCode = "REG_TEL_03";

    public const string UnknownPhoneCountryCodeMessage =
        "No se pudo identificar la característica del país del teléfono.";
}
