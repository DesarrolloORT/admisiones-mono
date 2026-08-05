using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.Identity.Dtos;

/// <summary>
/// DTO con la información básica de la persona autenticada.
/// </summary>
[ExcludeFromCodeCoverage]
public class AuthenticatedPerson
{
    /// <summary>
    /// Código único de la persona.
    /// </summary>
    public long PersonId { get; set; }

    /// <summary>
    /// Primer nombre de la persona.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Segundo nombre de la persona.
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Primer apellido de la persona.
    /// </summary>
    public string? FirstSurname { get; set; }

    /// <summary>
    /// Segundo apellido de la persona.
    /// </summary>
    public string? SecondSurname { get; set; }

    /// <summary>
    /// Tipo de persona (ej: CONTACTO, EMPLEADO, etc.).
    /// </summary>
    public string? PersonType { get; set; }

    /// <summary>
    /// Documento de identidad.
    /// </summary>
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Email de la persona. Uso interno — no se expone en la respuesta. Usado para flujo 2FA.
    /// </summary>
    [JsonIgnore]
    public string? Email { get; set; }
}
