using AppLogic.Contracts.Text;
using AppLogic.Identity;
using AppLogic.Identity.Services;
using AppLogic.Registration.Constants;
using AppLogic.Registration.Dtos;
using System;
using BusinessLogic.Entities;

namespace AppLogic.Registration.Validators;

public static class RegistrationValidation
{
    /// <summary>
    /// Traduce el error de <see cref="IdentityDocumentRules.ValidateBaseDocument"/> al código que
    /// espera el front para el registro.
    /// </summary>
    public static string ResolveDocumentValidationCode(IdentityDocumentRules.DocumentValidationError error) =>
        IdentityDocumentService.ResolveDocumentValidationCode(
            error,
            RegistrationErrorCodes.InvalidDocumentType,
            RegistrationErrorCodes.InvalidDocument);

    /// <summary>
    /// Los datos ingresados en el registro deben coincidir con la persona que ya existe en el
    /// padrón: tipo y número de documento, primer apellido y mail.
    /// Un solo estado de fallo, así que el tipo natural es <c>bool</c>; el mensaje y el código
    /// los pone el caso de uso.
    /// </summary>
    public static bool MatchesExistingPerson(Persona person, VerifyIdentityRequest request)
    {
        var inputSurname = TextNormalization.ToUpperWithoutAccents(request.FirstSurname);
        var personSurname = !string.IsNullOrWhiteSpace(person.PrimerApellidoMay)
            ? TextNormalization.Trim(person.PrimerApellidoMay)
            : TextNormalization.ToUpperWithoutAccents(person.PrimerApellido);

        return TextNormalization.Trim(person.TipoDocumento) == TextNormalization.Trim(request.DocumentType)
            && TextNormalization.Trim(person.Documento) == TextNormalization.Trim(request.DocumentNumber)
            && personSurname == inputSurname
            && string.Equals(
                TextNormalization.Trim(person.Email),
                TextNormalization.Trim(request.Email),
                StringComparison.OrdinalIgnoreCase);
    }
}
