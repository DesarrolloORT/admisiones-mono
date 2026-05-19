using AppLogic.DTOs;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.IServices;

public interface IPasswordActivationService
{
    Task<OperationResult<object?>> EnviarMailLinkPasswordAsync(Persona persona, string originMethod);
    Task<OperationResult<DtoPasswordActivationSession>> ActivarLinkPasswordAsync(string token);
    OperationResult<long> ValidarSessionToken(string sessionToken);
}
