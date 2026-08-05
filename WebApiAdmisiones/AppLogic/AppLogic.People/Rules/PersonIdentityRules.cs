using AppLogic.Contracts.Text;
using AppLogic.People.Dtos;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.People.Rules;

public static class PersonIdentityRules
{
    public static bool HasRestrictedIdentity(Persona person, bool tieneInscripcionActiva)
    {
        var activeStaffMember = TextNormalization.IsYes(person.FuncionarioActivoPersona);
        var usoExclusivoDba = TextNormalization.IsYes(person.UsoexclusivodbaPersona);
        if (activeStaffMember || usoExclusivoDba)
        {
            return true;
        }

        return TextNormalization.IsYes(person.AlumnoExtranjeroPersona)
            || tieneInscripcionActiva;
    }

    /// <summary>
    /// Con identidad restringida, ningún dato de identidad puede cambiar.
    /// Un solo estado de fallo, así que el tipo natural es <c>bool</c>.
    /// </summary>
    public static bool AreIdentityChangesAllowed(
        Persona person,
        UpdatePersonDetailsRequest request,
        bool identidadRestringida)
    {
        if (!identidadRestringida)
        {
            return true;
        }

        return !(TextChanged(request.DocumentType, person.TipoDocumento)
            || TextChanged(request.DocumentNumber, person.Documento)
            || TextChanged(request.FirstName, person.PrimerNombre)
            || TextChanged(request.MiddleName, person.SegundoNombre)
            || TextChanged(request.FirstSurname, person.PrimerApellido)
            || TextChanged(request.SecondSurname, person.SegundoApellido)
            || DateChanged(request.BirthDate, person.FechaNacimiento)
            || TextChanged(request.Sex, person.Sexo));
    }

    public static void ApplyIdentityChanges(
        Persona person,
        UpdatePersonDetailsRequest request,
        bool identidadRestringida)
    {
        if (identidadRestringida)
        {
            return;
        }

        if (request.DocumentType is not null)
        {
            person.TipoDocumento = TextNormalization.ToUpperWithoutAccents(request.DocumentType);
        }

        if (request.DocumentNumber is not null)
        {
            person.Documento = TextNormalization.Trim(request.DocumentNumber);
        }

        if (request.FirstName is not null)
        {
            person.PrimerNombre = TextNormalization.ToTitleCase(request.FirstName);
            person.PrimerNombreMay = person.PrimerNombre.ToUpperInvariant();
        }

        if (request.MiddleName is not null)
        {
            person.SegundoNombre = TextNormalization.ToTitleCaseOrNull(request.MiddleName);
            person.SegundoNombreMay = person.SegundoNombre?.ToUpperInvariant();
        }

        if (request.FirstSurname is not null)
        {
            person.PrimerApellido = TextNormalization.ToTitleCase(request.FirstSurname);
            person.PrimerApellidoMay = person.PrimerApellido.ToUpperInvariant();
        }

        if (request.SecondSurname is not null)
        {
            person.SegundoApellido = TextNormalization.ToTitleCaseOrNull(request.SecondSurname);
            person.SegundoApellidoMay = person.SegundoApellido?.ToUpperInvariant();
        }

        if (request.BirthDate.HasValue)
        {
            person.FechaNacimiento = request.BirthDate.Value.Date;
        }

        if (request.Sex is not null)
        {
            person.Sexo = string.IsNullOrWhiteSpace(request.Sex)
                ? null
                : TextNormalization.ToUpperWithoutAccents(request.Sex);
        }
    }

    private static bool TextChanged(string? valorNuevo, string? valorActual)
    {
        return valorNuevo is not null
            && !string.Equals(TextNormalization.Trim(valorNuevo), TextNormalization.Trim(valorActual), StringComparison.OrdinalIgnoreCase);
    }

    private static bool DateChanged(DateTime? valorNuevo, DateTime? valorActual)
    {
        return valorNuevo.HasValue
            && (!valorActual.HasValue || valorNuevo.Value.Date != valorActual.Value.Date);
    }

}
