using AppLogic.Contracts.Redaction;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Authentication.Dtos;

/// <summary>
/// DTO para la solicitud de autenticación de usuario.
/// </summary>
[ExcludeFromCodeCoverage]
public class AuthRequest
{
    /// <summary>
    /// Tipo de documento de la persona que intenta autenticarse.
    /// </summary>
    [Required(ErrorMessage = "El tipo de documento es requerido.")]
    public string? DocumentType { get; set; }

    /// <summary>
    /// Número de documento de la persona que intenta autenticarse.
    /// </summary>
    [Required(ErrorMessage = "El número de documento es requerido.")]
    public string? DocumentNumber { get; set; }

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
public class RecoverPasswordRequest
{
    /// <summary>
    /// Tipo de documento de la persona.
    /// </summary>
    [Required(ErrorMessage = "El tipo de documento   es requerido.")]
    public string? DocumentType { get; set; }

    /// <summary>
    /// Numero de documento de la persona.
    /// </summary>
    [Required(ErrorMessage = "El número de documento es requerido.")]
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Primer apellido de la persona.
    /// </summary>
    [Required(ErrorMessage = "El primer apellido es requerido.")]
    public string? FirstSurname { get; set; }
}

/// <summary>
/// DTO para validar el link de creacion de password.
/// </summary>
[ExcludeFromCodeCoverage]
public class ActivatePasswordLinkRequest
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
public class CompleteInitialPasswordRequest
{
    /// <summary>
    /// Nueva password a establecer.
    /// </summary>
    [Required(ErrorMessage = "La nueva password es requerida.")]
    [Redact]
    public required string NewPassword { get; set; }
}

/// <summary>
/// DTO para completar la verificación de dos factores.
/// </summary>
[ExcludeFromCodeCoverage]
public class VerifyTwoFactorCodeRequest
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
    public required string Code { get; set; }
}

/// <summary>
/// DTO para reenviar el código de dos factores a la misma sesión.
/// </summary>
[ExcludeFromCodeCoverage]
public class ResendTwoFactorCodeRequest
{
    /// <summary>
    /// Identificador de sesión 2FA devuelto por el endpoint de Login.
    /// </summary>
    [Required(ErrorMessage = "El session ID es requerido.")]
    public required string SessionId { get; set; }
}
