using AppLogic.Contracts.Dtos;
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
            PrimaryPhone = ToPhoneNumber(person.Telefono1),
            Email = mail,
            EmailConfirmation = mail,
            HasRestrictedIdentity = identidadRestringida
        };
    }

    /// <summary>
    /// Desglosa el teléfono guardado en el mismo <c>PhoneNumber</c> que el front manda al actualizar,
    /// para que pueda precargar el selector de país sin parsear el E.164. El país sale del prefijo del
    /// número, así que no hace falta la fila de <c>T_CARACTERISTICA_PAIS</c>.
    /// Un dato legacy en formato local (sin '+') o un fijo no valida como celular: vuelve crudo en
    /// <c>NationalNumber</c> con <c>IsValid = false</c>, que es la señal de que el front tiene que
    /// volver a pedir el país antes de guardar, en vez de comerse un PER_ADP_07 al hacer el PUT.
    /// </summary>
    private static PhoneNumber ToPhoneNumber(string? stored)
    {
        var raw = TextNormalization.Trim(stored);
        var phone = PhoneNormalization.Validate(raw, isPrimaryPhone: true, iso2: null);

        return phone?.TelefonoValido != true
            ? new PhoneNumber { NationalNumber = raw }
            : new PhoneNumber
            {
                IsValid = true,
                E164 = phone.TelefonoE164,
                Iso2 = phone.Iso2,
                CountryCode = phone.Caracteristica,
                NationalNumber = phone.TelefonoSimple
            };
    }
}
