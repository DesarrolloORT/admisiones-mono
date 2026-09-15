using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Authentication.Dtos;

/// <summary>
/// Respuesta cuando el login requiere verificación de dos factores.
/// El cliente debe solicitar el código al usuario y llamar a POST /Auth/VerifyTwoFactorCode.
/// </summary>
[ExcludeFromCodeCoverage]
public class TwoFactorRequiredResponse
{
    /// <summary>
    /// Siempre true. Indica que se requiere verificación adicional.
    /// </summary>
    public static bool RequiresTwoFactor => true;

    /// <summary>
    /// Identificador de sesión temporal. Debe enviarse junto con el código en VerifyTwoFactorCode.
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Email enmascarado al que se envio el codigo de verificacion.
    /// </summary>
    public string MaskedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje informativo para el usuario.
    /// </summary>
    public string Message { get; set; } = "Se envió un código de verificación a tu correo electrónico.";
}
