using AppLogic.Identity.Dtos;
using AppLogic.Authentication.Dtos;
using Utilities;

namespace AppLogic.Authentication.Interfaces;

/// <summary>
/// Servicio de autenticación de dos factores (2FA) por email.
/// Gestiona la sesión temporal de verificación y el envío de códigos.
/// </summary>
public interface ITwoFactorAuthService
{
    /// <summary>
    /// Inicia el flujo 2FA: genera un código, lo almacena en Redis y lo envía por email.
    /// La sesión guarda solo identidad verificada, no tokens estos se emiten recién
    /// en <see cref="VerifyCodeAsync"/> cuando el código es correcto.
    /// </summary>
    Task<OperationResult<TwoFactorRequiredResponse>> StartAsync(AuthenticatedPerson person, string email);

    /// <summary>
    /// Verifica el código ingresado por el usuario.
    /// Si es válido, retorna los tokens para establecer las cookies de sesión.
    /// </summary>
    Task<OperationResult<AuthenticationResponse>> VerifyCodeAsync(string sessionId, string code);

    /// <summary>
    /// Reenvía un nuevo código de verificación para una sesión 2FA vigente.
    /// </summary>
    Task<OperationResult<TwoFactorRequiredResponse>> ResendCodeAsync(string sessionId);
}
