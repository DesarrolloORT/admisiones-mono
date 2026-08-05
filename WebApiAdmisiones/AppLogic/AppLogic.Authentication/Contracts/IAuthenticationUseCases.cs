using AppLogic.Authentication.Dtos;
using AppLogic.Identity.Dtos;
using Utilities;

namespace AppLogic.Authentication.Contracts;

/// <summary>
/// Valida credenciales contra LDAP y devuelve la identidad de la persona autenticada.
/// NO emite ni persiste tokens: eso es decisión del llamador, que debe pasar por
/// <see cref="IIssueTokensForPerson"/> recién cuando el login esté completo (sin 2FA pendiente).
/// </summary>
public interface IAuthenticateWithLdap
{
    Task<OperationResult<AuthenticatedPerson>> ExecuteAsync(string documentType, string document, string pass);
}

/// <summary>
/// Genera y persiste los tokens de sesión (access + refresh) para una persona ya identificada.
/// Único punto de emisión de tokens del flujo de login.
/// </summary>
public interface IIssueTokensForPerson
{
    Task<OperationResult<AuthenticationResponse>> ExecuteAsync(long personId, string? message = null);
}

/// <summary>Renueva access y refresh token a partir de un refresh token válido.</summary>
public interface IRefreshTokens
{
    Task<OperationResult<AuthenticationResponse>> ExecuteAsync(string? refreshToken);
}

/// <summary>
/// Dispara el mail de recuperación de contraseña. Responde siempre lo mismo exista o no la persona:
/// el endpoint es público y no debe permitir enumerar cuentas.
/// </summary>
public interface IRecoverPassword
{
    Task<OperationResult<object>> ExecuteAsync(RecoverPasswordRequest request);
}

/// <summary>
/// Orquesta CompleteInitialPassword: valida la sesión temporal y despacha al flujo de persona nueva
/// (Redis) o persona existente. Devuelve además si el controller debe limpiar la cookie temporal de
/// activación (decisión HTTP que el controller sigue aplicando).
/// </summary>
public interface ICompletePasswordFlow
{
    Task<CompletePasswordFlowResult> ExecuteAsync(string? sessionToken, CompleteInitialPasswordRequest request);
}
