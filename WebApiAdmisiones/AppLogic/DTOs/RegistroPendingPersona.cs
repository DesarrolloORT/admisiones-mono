using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs;

/// <summary>
/// Datos de una nueva persona pendiente de creación en t_persona,
/// almacenados temporalmente en Redis hasta que el usuario establece su contraseña.
/// Redis key: registro:pending:{FlowId}   TTL = PasswordActivation:ExpireHours.
/// Redis index: registro:pending-doc:{TipoDocumento}:{Documento} -> FlowId.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RegistroPendingPersona
{
    public string FlowId { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;

    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public string PrimerNombre { get; set; } = string.Empty;
    public string? SegundoNombre { get; set; }

    public DateTime FechaNacimiento { get; set; }
    public string Sexo { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;
    public string Telefono1 { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public long CodigoPais { get; set; }
    public long CodigoEstado { get; set; }
    public long CodigoCiudad { get; set; }

    /// <summary>Hash SHA256 del JWT de activación para comparar cuando el usuario abre el link.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
