using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Identity.Dtos;

/// <summary>
/// Datos de una nueva persona pendiente de creación en t_persona,
/// almacenados temporalmente en Redis hasta que el usuario establece su contraseña.
/// Redis key: registro:pending:{FlowId}   TTL = PasswordActivation:ExpireHours.
/// Redis index: registro:pending-doc:{DocumentType}:{DocumentNumber} -> FlowId.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PendingPerson
{
    /// <summary>Identificador de la sesión de registro en curso.</summary>
    public string FlowId { get; set; } = string.Empty;

    /// <summary>Tipo de documento: CI, DE, PS o CC.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de documento.</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Primer apellido.</summary>
    public string FirstSurname { get; set; } = string.Empty;

    /// <summary>Segundo apellido.</summary>
    public string? SecondSurname { get; set; }

    /// <summary>Primer nombre.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Segundo nombre.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Fecha de nacimiento.</summary>
    public DateTime BirthDate { get; set; }

    /// <summary>Sexo según el documento.</summary>
    public string Sex { get; set; } = string.Empty;

    /// <summary>Domicilio.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Teléfono principal.</summary>
    public string PrimaryPhone { get; set; } = string.Empty;

    /// <summary>Email de contacto, al que se envía el link de activación.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Código de país de residencia.</summary>
    public long CountryId { get; set; }

    /// <summary>Código de estado o provincia de residencia.</summary>
    public long StateId { get; set; }

    /// <summary>Código de ciudad de residencia.</summary>
    public long CityId { get; set; }

    /// <summary>Hash SHA256 del JWT de activación para comparar cuando el usuario abre el link.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Momento en que se guardó el registro pendiente.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
