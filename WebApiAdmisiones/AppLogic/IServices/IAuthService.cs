using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices;

public interface IAuthService
{
    Task<OperationResult<DtoAuthenticationResponse>> AutenticarUsuarioLDAPAsync(long codigoPersona, string pass);
    Task<OperationResult<DtoAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken);
    Task<OperationResult<object>> RecuperarPassword(DtoRecuperarPasswordRequest request);
    Task<OperationResult<object>> CambiarPasswordAsync(long codigoPersona, DtoCambiarPasswordRequest request);
    Task<OperationResult<DtoAuthenticationResponse>> CompletarPasswordInicialAsync(long codigoPersona, DtoCompletarPasswordInicialRequest request);
}
