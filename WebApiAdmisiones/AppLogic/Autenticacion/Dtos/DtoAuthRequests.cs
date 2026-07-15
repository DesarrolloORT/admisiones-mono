using AppLogic.Common.Security;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Autenticacion.Dtos
{
    /// <summary>
    /// DTO para la solicitud de autenticación de usuario.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoAuthRequest
    {
        /// <summary>
        /// Tipo de documento de la persona que intenta autenticarse.
        /// </summary>
        [Required(ErrorMessage = "El tipo de documento es requerido.")]
        public string? TipoDocumento { get; set; }

        /// <summary>
        /// Número de documento de la persona que intenta autenticarse.
        /// </summary>
        [Required(ErrorMessage = "El número de documento es requerido.")]
        public string? Documento { get; set; }

        /// <summary>
        /// Contraseña del usuario.
        /// </summary>
        [Required(ErrorMessage = "La contraseña es requerida.")]
        [Redact]
        public required string Password { get; set; }
    }

    /// <summary>
    /// DTO para la recuperacion de password.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoRecuperarPasswordRequest
    {
        /// <summary>
        /// Tipo de documento de la persona.
        /// </summary>
        [Required(ErrorMessage = "El tipo de documento   es requerido.")]
        public string? TipoDocumento { get; set; }

        /// <summary>
        /// Numero de documento de la persona.
        /// </summary>
        [Required(ErrorMessage = "El número de documento es requerido.")]
        public string? Documento { get; set; }

        /// <summary>
        /// Primer apellido de la persona.
        /// </summary>
        [Required(ErrorMessage = "El primer apellido es requerido.")]
        public string? PrimerApellido { get; set; }
    }

    /// <summary>
    /// DTO para el cambio de password del usuario autenticado.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoCambiarPasswordRequest
    {
        /// <summary>
        /// Password actual del usuario.
        /// </summary>
        [Required(ErrorMessage = "La password actual es requerida.")]
        [Redact]
        public required string PasswordActual { get; set; }

        /// <summary>
        /// Nueva password a establecer.
        /// </summary>
        [Required(ErrorMessage = "La nueva password es requerida.")]
        [Redact]
        public required string PasswordNueva { get; set; }
    }

    /// <summary>
    /// DTO para validar el link de creacion de password.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoActivarLinkPasswordRequest
    {
        /// <summary>
        /// JWT de activacion recibido por mail.
        /// </summary>
        [Required(ErrorMessage = "El token es requerido.")]
        [Redact]
        public required string Token { get; set; }
    }

    /// <summary>
    /// DTO para completar la password inicial usando la sesion temporal.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoCompletarPasswordInicialRequest
    {
        /// <summary>
        /// Nueva password a establecer.
        /// </summary>
        [Required(ErrorMessage = "La nueva password es requerida.")]
        [Redact]
        public required string PasswordNueva { get; set; }
    }

    /// <summary>
    /// DTO para completar la verificación de dos factores.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoVerificarCodigo2FARequest
    {
        /// <summary>
        /// Identificador de sesión 2FA devuelto por el endpoint de Login.
        /// </summary>
        [Required(ErrorMessage = "El session ID es requerido.")]
        public required string SessionId { get; set; }

        /// <summary>
        /// Código de verificación recibido por email.
        /// </summary>
        [Required(ErrorMessage = "El código es requerido.")]
        [Redact]
        public required string Codigo { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class DtoReenviarCodigo2FARequest
    {
        [Required(ErrorMessage = "El session ID es requerido.")]
        public required string SessionId { get; set; }
    }
}
