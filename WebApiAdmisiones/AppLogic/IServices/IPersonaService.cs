using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices;

/// <summary>
/// Interfaz para el servicio de autenticación de usuarios.
/// </summary>
public interface IPersonaService
{
    /// <summary>
    /// Autentica un usuario contra el servicio LDAP.
    /// </summary>
    /// <param name="codigoPersona">Código de la persona a autenticar.</param>
    /// <param name="pass">Contraseña del usuario.</param>
    /// <returns>OperationResult con la respuesta de autenticación incluyendo tokens y la Persona autenticada si el login es exitoso.</returns>
    Task<OperationResult<DTOAuthenticationResponse>> AutenticarUsuarioLDAPAsync(long codigoPersona, string pass);

    /// <summary>
    /// Refresca los tokens de autenticación usando el refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token enviado por el cliente.</param>
    /// <param name="codigoPersonaClaim">Código de persona extraído del access token actual.</param>
    /// <returns>OperationResult con los nuevos tokens generados.</returns>
    Task<OperationResult<DTOAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken, string? codigoPersonaClaim);
}
