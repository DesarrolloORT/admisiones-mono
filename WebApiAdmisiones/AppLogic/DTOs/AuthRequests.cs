using AppLogic.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    /// <summary>
    /// DTO para la solicitud de autenticación de usuario.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AuthRequest
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
    }
}
