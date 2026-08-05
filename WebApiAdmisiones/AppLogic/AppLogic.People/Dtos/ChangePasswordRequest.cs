using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using AppLogic.Contracts.Redaction;

namespace AppLogic.People.Dtos;

/// <summary>
/// DTO para el cambio de password del usuario autenticado.
/// </summary>
[ExcludeFromCodeCoverage]
public class ChangePasswordRequest
{
    /// <summary>
    /// Password actual del usuario.
    /// </summary>
    [Required(ErrorMessage = "La password actual es requerida.")]
    [Redact]
    public required string CurrentPassword { get; set; }

    /// <summary>
    /// Nueva password a establecer.
    /// </summary>
    [Required(ErrorMessage = "La nueva password es requerida.")]
    [Redact]
    public required string NewPassword { get; set; }
}
