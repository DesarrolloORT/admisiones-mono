using System;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.People.Dtos;

/// <summary>Datos personales de la persona autenticada, tal como los muestra Autoservicio.</summary>
[ExcludeFromCodeCoverage]
public class PersonDetailsResponse
{
    /// <summary>Tipo de documento: CI, PS o DE.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de documento.</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Primer nombre.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Segundo nombre.</summary>
    public string MiddleName { get; set; } = string.Empty;

    /// <summary>Primer apellido.</summary>
    public string FirstSurname { get; set; } = string.Empty;

    /// <summary>Segundo apellido.</summary>
    public string SecondSurname { get; set; } = string.Empty;

    /// <summary>Fecha de nacimiento.</summary>
    public DateTime BirthDate { get; set; }

    /// <summary>Sexo: M o F.</summary>
    public string Sex { get; set; } = string.Empty;

    /// <summary>País del domicilio.</summary>
    public long CountryId { get; set; }

    /// <summary>Estado o departamento del domicilio.</summary>
    public long StateId { get; set; }

    /// <summary>Ciudad del domicilio.</summary>
    public long CityId { get; set; }

    /// <summary>Dirección del domicilio.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Teléfono principal de contacto.</summary>
    public string PrimaryPhone { get; set; } = string.Empty;

    /// <summary>Mail de contacto.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Se devuelve igual a <see cref="Email"/> para precargar el campo de confirmación del formulario.</summary>
    public string EmailConfirmation { get; set; } = string.Empty;

    /// <summary>
    /// La persona tiene la identidad restringida: el front debe mostrar documento, nombres, fecha de
    /// nacimiento y sexo como solo lectura, porque el backend ignora cualquier cambio a esos campos.
    /// </summary>
    public bool HasRestrictedIdentity { get; set; }
}
