namespace AppLogic.DTOs;

/// <summary>
/// Modelo de sesión 2FA almacenada en Redis durante el flujo de verificación.
/// Contiene los tokens pendientes y el hash del código de verificación.
/// </summary>
public sealed class TwoFactorSession
{
    public long CodigoPersona { get; set; }
    public string? PrimerNombre { get; set; }
    public string? SegundoNombre { get; set; }
    public string? PrimerApellido { get; set; }
    public string? SegundoApellido { get; set; }
    public string? TipoPersona { get; set; }
    public string? Documento { get; set; }

    // Tokens pendientes de establecer como cookies tras la verificación
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? RefreshTokenHash { get; set; }

    // Datos de verificación
    public string? CodigoHash { get; set; }
    public DateTime CodigoExpiresAtUtc { get; set; }
    public string? Email { get; set; }
    public int Intentos { get; set; }
}
