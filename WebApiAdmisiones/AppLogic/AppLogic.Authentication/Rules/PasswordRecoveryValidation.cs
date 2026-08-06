using AppLogic.Contracts.Text;
using BusinessLogic.Entities;

namespace AppLogic.Authentication.Rules;

public static class PasswordRecoveryValidation
{
    /// <summary>
    /// Los datos ingresados en el recupero deben coincidir con la persona del padrón: tipo y número
    /// de documento más primer apellido. Un solo estado de fallo, así que el tipo natural es
    /// <c>bool</c>; el caso de uso decide qué responder (siempre el mensaje genérico).
    /// </summary>
    public static bool MatchesPerson(Persona person, string documentType, string document, string? firstSurname)
    {
        if (string.IsNullOrWhiteSpace(firstSurname))
        {
            return false;
        }

        var inputSurname = TextNormalization.ToUpperWithoutAccents(firstSurname);
        var personSurname = !string.IsNullOrWhiteSpace(person.PrimerApellidoMay)
            ? TextNormalization.Trim(person.PrimerApellidoMay)
            : TextNormalization.ToUpperWithoutAccents(person.PrimerApellido);

        return TextNormalization.Trim(person.TipoDocumento) == documentType
            && TextNormalization.Trim(person.Documento) == document
            && personSurname == inputSurname;
    }
}
