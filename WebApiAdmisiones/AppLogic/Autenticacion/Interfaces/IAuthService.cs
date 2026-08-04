using AppLogic.Autenticacion.Dtos;
using Utilities;

namespace AppLogic.Autenticacion.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Valida credenciales contra LDAP y devuelve la identidad de la persona autenticada.
    /// NO emite ni persiste tokens: la emisión de tokens es una decisión del llamador,
    /// que debe pasar por <see cref="GenerarTokensParaPersonaAsync"/> recién cuando el login esté
    /// completo (sin 2FA pendiente).
    /// </summary>
    Task<OperationResult<DtoPersonaAuth>> AutenticarUsuarioLDAPAsync(string tipoDocumento, string documento, string pass);

    /// <summary>
    /// Genera y persiste los tokens de sesión (access + refresh) para una persona ya identificada.
    /// Único punto de emisión de tokens del flujo de login.
    /// </summary>
    Task<OperationResult<DtoAuthenticationResponse>> GenerarTokensParaPersonaAsync(long codigoPersona, string? message = null);

    Task<OperationResult<DtoAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken);
    Task<OperationResult<object>> RecuperarPassword(DtoRecuperarPasswordRequest request);
    /// <summary>
    /// Orquesta CompletarPassword completo: valida la sesión temporal y despacha al flujo de
    /// persona nueva (Redis) o persona existente. Devuelve además si el controller debe limpiar
    /// la cookie temporal de activación (decisión HTTP que el controller sigue aplicando).
    /// </summary>
    Task<DtoCompletarPasswordFlowResult> CompletarPasswordFlowAsync(
        string? sessionToken,
        DtoCompletarPasswordInicialRequest request);
}
