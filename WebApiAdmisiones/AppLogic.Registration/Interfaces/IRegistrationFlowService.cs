using AppLogic.Authentication.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.Registration.Dtos;
using Utilities;

namespace AppLogic.Registration.Interfaces;

/// <summary>
/// Extiende <see cref="IPendingRegistrationCompletion"/>: esa parte del contrato la consume
/// Authentication al completar el password inicial de una persona nueva.
/// </summary>
public interface IRegistrationFlowService : IPendingRegistrationCompletion
{
    /// <summary>
    /// Crea una sesión de registro (FlowId) en Redis para el documento evaluado.
    /// Se llama al final de EvaluateDocument y el FlowId se retorna al frontend.
    /// </summary>
    Task<string> CreateFlowSessionAsync(string documentType, string document, long? personId);

    /// <summary>
    /// Valida que exista una sesión activa para el FlowId y que el step sea el esperado.
    /// Retorna null si es válida; de lo contrario retorna el OperationResult de error.
    /// </summary>
    Task<OperationResult<object?>?> ValidateFlowSessionAsync(string? flowId, string stepEsperado);

    Task<OperationResult<object?>?> ValidateFlowDocumentAsync(
        string flowId,
        string documentType,
        string document,
        string originMethod);

    /// <summary>Avanza el step de la sesión al valor indicado.</summary>
    Task UpdateStepAsync(string flowId, string nuevoStep);

    /// <summary>
    /// Orquesta la confirmación de una nueva persona: valida, guarda en Redis y envía email.
    /// NO crea la persona en t_persona (se difiere a CompleteInitialPassword).
    /// </summary>
    Task<OperationResult<RegistrationFlowResult>> ConfirmNewPersonAsync(
        RegisterPersonRequest request,
        string flowId);

}

/// <summary>
/// Resultado de ConfirmNewPersonAsync. <see cref="MailSent"/> distingue éxito parcial
/// (registro OK, mail no enviado) de éxito completo.
/// </summary>
public record RegistrationFlowResult(string Message, bool MailSent = true);
