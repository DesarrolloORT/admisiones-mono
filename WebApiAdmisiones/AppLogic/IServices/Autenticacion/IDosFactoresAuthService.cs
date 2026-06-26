using AppLogic.Dtos.Autenticacion;
using Utilities;

namespace AppLogic.IServices.Autenticacion;

/// <summary>
/// Servicio de autenticación de dos factores (2FA) por email.
/// Gestiona la sesión temporal de verificación y el envío de códigos.
/// </summary>
public interface IDosFactoresAuthService
{
    /// <summary>
    /// Inicia el flujo 2FA: genera un código, lo almacena en Redis y lo envía por email.
    /// </summary>
    Task<OperationResult<DtoLogin2FARequired>> IniciarAsync(DtoAuthenticationResponse pendingAuth, string email);

    /// <summary>
    /// Verifica el código ingresado por el usuario.
    /// Si es válido, retorna los tokens para establecer las cookies de sesión.
    /// </summary>
    Task<OperationResult<DtoAuthenticationResponse>> VerificarCodigoAsync(string sessionId, string codigo);

    /// <summary>
    /// Reenvía un nuevo código de verificación para una sesión 2FA vigente.
    /// </summary>
    Task<OperationResult<DtoLogin2FARequired>> ReenviarCodigoAsync(string sessionId);
}
