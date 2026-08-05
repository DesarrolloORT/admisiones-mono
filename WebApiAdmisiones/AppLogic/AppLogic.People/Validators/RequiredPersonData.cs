using AppLogic.People.Constants;
using BusinessLogic.Entities;

namespace AppLogic.People.Validators;

/// <summary>
/// Comprueba qué dato obligatorio falta para confirmar los datos personales.
/// Devuelve el faltante (<see cref="PersonDataGap.None"/> si está completo); armar la respuesta
/// HTTP es responsabilidad del caso de uso.
///
/// <para>
/// ⚠️ Sin consumidor en producción hoy: solo lo ejercitan sus tests. Se conserva porque describe
/// reglas de negocio vigentes, pero conviene decidir si se cablea o se elimina.
/// </para>
/// </summary>
public static class RequiredPersonData
{
    public static PersonDataGap FindMissingRequiredData(Persona person)
    {
        var basicos = ValidateBasicsComplete(person);
        if (basicos != PersonDataGap.None) return basicos;

        var document = ValidateDocumentComplete(person);
        if (document != PersonDataGap.None) return document;

        var address = ValidateAddressComplete(person);
        if (address != PersonDataGap.None) return address;

        return ValidateContactComplete(person);
    }

    private static PersonDataGap ValidateBasicsComplete(Persona person)
    {
        if (person.CodigoPaisNacimiento == null)
            return PersonDataGap.BirthCountryMissing;
        if (string.IsNullOrWhiteSpace(person.NacionalidadPersona)
            && string.Equals(person.FuncionarioActivoPersona, "SI", StringComparison.OrdinalIgnoreCase))
            return PersonDataGap.NationalityMissing;
        return PersonDataGap.None;
    }

    private static PersonDataGap ValidateDocumentComplete(Persona person)
    {
        if (!person.FechaVtoDocumentoPersona.HasValue || person.FechaVtoDocumentoPersona.Value <= DateTime.MinValue)
            return PersonDataGap.DocumentExpiryMissing;
        return PersonDataGap.None;
    }

    private static PersonDataGap ValidateAddressComplete(Persona person)
    {
        if (person.CodigoPais == null || person.CodigoPais <= 0)
            return PersonDataGap.ResidenceCountryMissing;
        if (person.CodigoEstado == null || person.CodigoEstado <= 0)
            return PersonDataGap.StateMissing;
        if (person.CodigoCiudad == null || person.CodigoCiudad <= 0)
            return PersonDataGap.CityMissing;
        if (string.IsNullOrWhiteSpace(person.Direccion))
            return PersonDataGap.AddressMissing;
        return PersonDataGap.None;
    }

    private static PersonDataGap ValidateContactComplete(Persona person)
    {
        if (string.IsNullOrWhiteSpace(person.Email))
            return PersonDataGap.EmailMissing;
        if (string.IsNullOrWhiteSpace(person.Telefono1))
            return PersonDataGap.PrimaryPhoneMissing;
        if (person.IdCaracteristicaPaisTel1 <= 0)
            return PersonDataGap.PhoneCountryCodeMissing;
        // Tel2 opcional, pero si existe característica sin teléfono podría ser inconsistente: no se valida estrictamente
        return PersonDataGap.None;
    }
}
