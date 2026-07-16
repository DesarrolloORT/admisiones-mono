using AppLogic.Autenticacion.Dtos;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Autenticacion.Interfaces;

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

    /// <summary>
    /// Genera el JWT de activación para un flujo pendiente (sub = flowId). Lo consume
    /// RegistroFlowService al confirmar una persona nueva (registro diferido vía Redis).
    /// </summary>
    string GenerarTokenFlowId(string flowId, string purpose, TimeSpan duration);
}
