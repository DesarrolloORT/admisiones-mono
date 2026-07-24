using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.Autenticacion.Dtos;

/// <summary>
/// DTO con la información básica de la persona autenticada.
/// </summary>
[ExcludeFromCodeCoverage]
public class DtoPersonaAuth
{
    /// <summary>
    /// Código único de la persona.
    /// </summary>
    public long CodigoPersona { get; set; }

    /// <summary>
    /// Primer nombre de la persona.
    /// </summary>
    public string? PrimerNombre { get; set; }

    /// <summary>
    /// Segundo nombre de la persona.
    /// </summary>
    public string? SegundoNombre { get; set; }

    /// <summary>
    /// Primer apellido de la persona.
    /// </summary>
    public string? PrimerApellido { get; set; }

    /// <summary>
    /// Segundo apellido de la persona.
    /// </summary>
    public string? SegundoApellido { get; set; }

    /// <summary>
    /// Tipo de persona (ej: CONTACTO, EMPLEADO, etc.).
    /// </summary>
    public string? TipoPersona { get; set; }

    /// <summary>
    /// Documento de identidad.
    /// </summary>
    public string? Documento { get; set; }

    /// <summary>
    /// Email de la persona. Uso interno — no se expone en la respuesta. Usado para flujo 2FA.
    /// </summary>
    [JsonIgnore]
    public string? Email { get; set; }
}
