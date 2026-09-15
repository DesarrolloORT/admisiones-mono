using AppLogic.Contracts.Dtos;
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

    /// <summary>
    /// Teléfono principal de contacto: celular obligatorio. Se guarda en E.164 armado con
    /// <see cref="PhoneNumber.NationalNumber"/> y <see cref="PhoneNumber.Iso2"/>.
    /// </summary>
    public PhoneNumber PrimaryPhone { get; set; } = new();

    /// <summary>Mail de contacto.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Repetición del mail: tiene que coincidir con <see cref="Email"/>.</summary>
    public string EmailConfirmation { get; set; } = string.Empty;
}

