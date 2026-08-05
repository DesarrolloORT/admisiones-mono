using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Registration.Dtos;

/// <summary>Primer paso del registro: qué documento quiere registrar la persona.</summary>
[ExcludeFromCodeCoverage]
public class EvaluateDocumentRequest
{
    /// <summary>Tipo de documento: CI, PS o DE.</summary>
    [Required]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de documento.</summary>
    [Required]
    public string DocumentNumber { get; set; } = string.Empty;
}

/// <summary>
/// Datos completos para dar de alta una persona nueva (cédula) o para dejar una solicitud de alta
/// (resto de los documentos, que revisa admisiones a mano).
/// </summary>
[ExcludeFromCodeCoverage]
public class RegisterPersonRequest
{
    /// <summary>Tipo de documento: CI, PS o DE.</summary>
    [Required]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de documento.</summary>
    [Required]
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Primer apellido.</summary>
    [Required]
    [MinLength(2)]
    public string FirstSurname { get; set; } = string.Empty;

    /// <summary>Segundo apellido.</summary>
    public string? SecondSurname { get; set; }

    /// <summary>Primer nombre.</summary>
    [Required]
    [MinLength(2)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Segundo nombre.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Fecha de nacimiento.</summary>
    [Range(typeof(DateTime), "1900-01-02", "9999-12-31")]
    public DateTime BirthDate { get; set; }

    /// <summary>Sexo: M o F.</summary>
    [Required]
    [RegularExpression("^[mMfF]$")]
    public string Sex { get; set; } = string.Empty;

    /// <summary>Dirección del domicilio.</summary>
    [Required]
    public string Address { get; set; } = string.Empty;

    /// <summary>Teléfono principal de contacto, en formato internacional.</summary>
    [Required]
    public string PrimaryPhone { get; set; } = string.Empty;

    /// <summary>Mail de contacto: es donde llega el link de activación de contraseña.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Repetición del mail: tiene que coincidir con <see cref="Email"/>.</summary>
    [Required]
    [Compare(nameof(Email))]
    public string EmailConfirmation { get; set; } = string.Empty;

    /// <summary>País del domicilio.</summary>
    [Range(1, long.MaxValue)]
    public long CountryId { get; set; }

    /// <summary>Estado o departamento del domicilio.</summary>
    [Range(1, long.MaxValue)]
    public long StateId { get; set; }

    /// <summary>Ciudad del domicilio.</summary>
    [Range(1, long.MaxValue)]
    public long CityId { get; set; }
}

/// <summary>
/// Verificación de identidad de una persona que ya está en el padrón: los datos tienen que coincidir
/// con los que ya tiene ORT registrados.
/// </summary>
[ExcludeFromCodeCoverage]
public class VerifyIdentityRequest
{
    /// <summary>Tipo de documento: solo aplica CI.</summary>
    [Required]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de documento.</summary>
    [Required]
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Primer apellido: tiene que coincidir con el del padrón.</summary>
    [Required]
    [MinLength(2)]
    public string FirstSurname { get; set; } = string.Empty;

    /// <summary>Mail: tiene que coincidir con el del padrón.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
