using AppLogic.Authentication.Dtos;
using BusinessLogic.Entities;
using Utilities;

namespace AppLogic.Authentication.Interfaces;

public interface IPasswordActivationService
{
    Task<OperationResult<object?>> SendPasswordLinkMailAsync(Persona person, string originMethod);
    Task<OperationResult<object?>> SendPasswordRecoveryMailAsync(Persona person, string originMethod);

    /// <summary>
    /// Envía el mail de activación para una nueva persona pendiente (sin Persona en DB).
    /// El hash del token se almacena en Redis junto con los datos de la persona pendiente.
    /// </summary>
    Task<OperationResult<object?>> SendNewPersonMailAsync(string flowId, string email, string token);

    Task<OperationResult<PasswordActivationSession>> ActivatePasswordLinkAsync(string token);

    /// <summary>Valida la sesión temporal y retorna el tipo de sesión + identificador.</summary>
    OperationResult<ValidatedSession> ValidateSessionToken(string sessionToken);

    /// <summary>
    /// Genera el JWT de activación para un flujo pendiente (sub = flowId). Lo consume
    /// RegistrationFlowService al confirmar una persona nueva (registro diferido vía Redis).
    /// </summary>
    string GenerateFlowIdToken(string flowId, string purpose, TimeSpan duration);
}
