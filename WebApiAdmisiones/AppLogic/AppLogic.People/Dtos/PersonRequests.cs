using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.People.Dtos;

/// <summary>
/// Actualización de los datos de la persona autenticada. Los datos de identidad (documento, nombres,
/// fecha de nacimiento y sexo) solo se aplican si la persona no tiene la identidad restringida.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdatePersonDetailsRequest
{
    /// <summary>Tipo de documento: CI, PS o DE.</summary>
    public string? DocumentType { get; set; }

    /// <summary>Número de documento.</summary>
    public string? DocumentNumber { get; set; }

    /// <summary>Primer nombre.</summary>
    public string? FirstName { get; set; }

    /// <summary>Segundo nombre.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Primer apellido.</summary>
    public string? FirstSurname { get; set; }

    /// <summary>Segundo apellido.</summary>
    public string? SecondSurname { get; set; }

    /// <summary>Fecha de nacimiento.</summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>Sexo: M o F.</summary>
    public string? Sex { get; set; }

    /// <summary>País del domicilio.</summary>
    public long CountryId { get; set; }

    /// <summary>Estado o departamento del domicilio.</summary>
    public long StateId { get; set; }

    /// <summary>Ciudad del domicilio.</summary>
    public long CityId { get; set; }

    /// <summary>Dirección del domicilio.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Teléfono principal de contacto, en formato internacional.</summary>
    public string PrimaryPhone { get; set; } = string.Empty;

    /// <summary>Mail de contacto.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Repetición del mail: tiene que coincidir con <see cref="Email"/>.</summary>
    public string EmailConfirmation { get; set; } = string.Empty;
}

/// <summary>
/// Teléfono desglosado por el validador del front. Se valida contra el formato del país antes de
/// aceptarlo como teléfono de contacto.
/// </summary>
[ExcludeFromCodeCoverage]
public class PhoneNumber
{
    /// <summary>Si el front ya lo dio por válido.</summary>
    public bool IsValid { get; set; }

    /// <summary>Número completo en formato E.164 (por ejemplo +59899123456).</summary>
    [StringLength(20)]
    public string? E164 { get; set; }

    /// <summary>Código de país ISO 3166-1 alfa-2 (por ejemplo UY).</summary>
    [StringLength(2)]
    public string? Iso2 { get; set; }

    /// <summary>Prefijo telefónico del país (por ejemplo 598).</summary>
    public long CountryCode { get; set; }

    /// <summary>Número sin el prefijo del país.</summary>
    [StringLength(20)]
    public string? NationalNumber { get; set; }
}
