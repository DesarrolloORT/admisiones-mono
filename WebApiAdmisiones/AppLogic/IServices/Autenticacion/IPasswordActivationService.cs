using AppLogic.DTOs;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.IServices.Autenticacion;

public interface IPasswordActivationService
{
    Task<OperationResult<object?>> EnviarMailLinkPasswordAsync(Persona persona, string originMethod);
    Task<OperationResult<object?>> EnviarMailRecuperacionPasswordAsync(Persona persona, string originMethod);

    /// <summary>
    /// Envía el mail de activación para una nueva persona pendiente (sin Persona en DB).
    /// El hash del token se almacena en Redis junto con los datos de la persona pendiente.
    /// </summary>
    Task<OperationResult<object?>> EnviarMailNuevaPersonaAsync(string flowId, string email, string token);

    Task<OperationResult<DtoPasswordActivationSession>> ActivarLinkPasswordAsync(string token);

    /// <summary>Valida la sesión temporal y retorna el tipo de sesión + identificador.</summary>
    OperationResult<DtoValidatedSession> ValidarSessionToken(string sessionToken);
}
