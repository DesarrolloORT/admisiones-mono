using AppLogic.Contracts.Text;
using AppLogic.People.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.People.Mapping;

/// <summary>
/// Traduce la entidad Persona al contrato público. Traducción pura: no consulta la base
/// ni decide nada de negocio; el flag de identidad restringida lo resuelve el caso de uso.
/// </summary>
internal static class PersonMapper
{
    internal static PersonDetailsResponse ToDetailsResponse(Persona person, bool identidadRestringida)
    {
        var mail = person.Email ?? string.Empty;
        return new PersonDetailsResponse
        {
            DocumentType = TextNormalization.Trim(person.TipoDocumento),
            DocumentNumber = TextNormalization.Trim(person.Documento),
            FirstName = TextNormalization.Trim(person.PrimerNombre),
            MiddleName = TextNormalization.Trim(person.SegundoNombre),
            FirstSurname = TextNormalization.Trim(person.PrimerApellido),
            SecondSurname = TextNormalization.Trim(person.SegundoApellido),
            BirthDate = person.FechaNacimiento ?? default,
            Sex = TextNormalization.Trim(person.Sexo),
            CountryId = person.CodigoPais ?? 0,
            StateId = person.CodigoEstado ?? 0,
            CityId = person.CodigoCiudad ?? 0,
            Address = TextNormalization.Trim(person.Direccion),
            PrimaryPhone = TextNormalization.Trim(person.Telefono1),
            Email = mail,
            EmailConfirmation = mail,
            HasRestrictedIdentity = identidadRestringida
        };
    }
}
