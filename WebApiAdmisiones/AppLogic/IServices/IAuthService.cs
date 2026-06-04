using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices;

public interface IAuthService
{
    Task<OperationResult<DtoAuthenticationResponse>> AutenticarUsuarioLDAPAsync(string tipoDocumento, string documento, string pass);
    Task<OperationResult<DtoAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken);
    Task<OperationResult<object>> RecuperarPassword(DtoRecuperarPasswordRequest request);
    Task<OperationResult<DtoAuthenticationResponse>> CompletarPasswordAsync(long codigoPersona, DtoCompletarPasswordInicialRequest request);

    /// <summary>
    /// Genera tokens de autenticación para una persona ya existente en DB.
    /// Usado después de completar el registro de una nueva persona.
    /// </summary>
    Task<OperationResult<DtoAuthenticationResponse>> GenerarTokensParaPersonaAsync(long codigoPersona);
}
