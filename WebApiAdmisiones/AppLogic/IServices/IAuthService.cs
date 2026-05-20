using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices;

public interface IAuthService
{
    Task<OperationResult<DtoAuthenticationResponse>> AutenticarUsuarioLDAPAsync(string tipoDocumento, string documento, string pass);
    Task<OperationResult<DtoAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken);
    Task<OperationResult<object>> RecuperarPassword(DtoRecuperarPasswordRequest request);
    Task<OperationResult<object>> CambiarPasswordAsync(long codigoPersona, DtoCambiarPasswordRequest request);
    Task<OperationResult<DtoAuthenticationResponse>> CompletarPasswordAsync(long codigoPersona, DtoCompletarPasswordInicialRequest request);
}
