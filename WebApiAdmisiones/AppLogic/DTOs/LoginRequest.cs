using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs;

/// <summary>
/// DTO para la solicitud de autenticación de usuario.
/// </summary>
[ExcludeFromCodeCoverage]
public class LoginRequest
{
    /// <summary>
    /// Código de la persona que intenta autenticarse.
    /// </summary>
    [Required(ErrorMessage = "El código de persona es requerido.")]
    public long CodigoPersona { get; set; }

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es requerida.")]
    public required string Password { get; set; }
}
