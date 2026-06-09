using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    /// <summary>
    /// Respuesta cuando el login requiere verificación de dos factores.
    /// El cliente debe solicitar el código al usuario y llamar a POST /Auth/VerificarCodigo2FA.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoLogin2FARequired
    {
        /// <summary>
        /// Siempre true. Indica que se requiere verificación adicional.
        /// </summary>
        public static bool RequiresTwoFactor => true;

        /// <summary>
        /// Identificador de sesión temporal. Debe enviarse junto con el código en VerificarCodigo2FA.
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
}
