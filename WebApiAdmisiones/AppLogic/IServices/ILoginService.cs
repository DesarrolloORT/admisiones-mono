using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices;

public interface ILoginService
{
    Task<OperationResult<DTOAuthenticationResponse>> AutenticarUsuarioLDAPAsync(long codigoPersona, string pass);
    Task<OperationResult<DTOAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken, string? codigoPersonaClaim);
}
