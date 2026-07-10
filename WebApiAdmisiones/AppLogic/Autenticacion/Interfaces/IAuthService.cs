using AppLogic.Autenticacion.Requests;
using AppLogic.Autenticacion.Responses;
using Utilities;

namespace AppLogic.Autenticacion.Interfaces;

public interface IAuthService
{
    Task<OperationResult<DtoAuthenticationResponse>> AutenticarUsuarioLDAPAsync(string tipoDocumento, string documento, string pass);
    Task<OperationResult<DtoAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken);
    Task<OperationResult<object>> RecuperarPassword(DtoRecuperarPasswordRequest request);
    Task<OperationResult<DtoAuthenticationResponse>> CompletarPasswordAsync(long codigoPersona, DtoCompletarPasswordInicialRequest request);
    Task<OperationResult<DtoAuthenticationResponse>> GenerarTokensParaPersonaAsync(long codigoPersona);
}
