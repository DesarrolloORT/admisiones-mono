using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.DTOs;

/// <summary>
/// Respuesta del endpoint que valida el link de creacion de password inicial.
/// </summary>
/// <remarks>
/// Por seguridad, el token de sesion temporal no se serializa en el body.
/// La API lo envia como cookie HttpOnly X-Password-Activation.
/// </remarks>
[ExcludeFromCodeCoverage]
public class DtoPasswordActivationSession
{
    /// <summary>
    /// Codigo de persona asociado al token de activacion validado.
    /// </summary>
    public long CodigoPersona { get; set; }

    /// <summary>
    /// Token de sesion temporal usado internamente para setear la cookie X-Password-Activation.
    /// </summary>
    /// <remarks>
    /// No se devuelve en Swagger ni en la respuesta JSON porque tiene JsonIgnore.
    /// </remarks>
    [JsonIgnore]
    public string? SessionToken { get; set; }

    /// <summary>
    /// Mensaje funcional para el frontend.
    /// </summary>
    public string Message { get; set; } = "Link validado correctamente.";
}
