using AppLogic.Autenticacion.Dtos;
using AppLogic.Autenticacion.Requests;
using AppLogic.Autenticacion.Responses;
using Utilities;

namespace AppLogic.Autenticacion.Interfaces;

public interface IAuthService
{
    Task<OperationResult<DtoAuthenticationResponse>> AutenticarUsuarioLDAPAsync(string tipoDocumento, string documento, string pass);
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
