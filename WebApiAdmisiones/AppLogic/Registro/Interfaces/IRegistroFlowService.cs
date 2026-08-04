using AppLogic.Registro.Dtos;
using Utilities;

namespace AppLogic.Registro.Interfaces;

public interface IRegistroFlowService
{
    /// <summary>
    /// Crea una sesión de registro (FlowId) en Redis para el documento evaluado.
    /// Se llama al final de EvaluarDocumento y el FlowId se retorna al frontend.
    /// </summary>
    Task<string> CrearFlowSessionAsync(string tipoDocumento, string documento, long? codigoPersona);

    /// <summary>
    /// Valida que exista una sesión activa para el FlowId y que el step sea el esperado.
    /// Retorna null si es válida; de lo contrario retorna el OperationResult de error.
    /// </summary>
    Task<OperationResult<object?>?> ValidarFlowSessionAsync(string? flowId, string stepEsperado);

    Task<OperationResult<object?>?> ValidarDocumentoFlowAsync(
        string flowId,
        string tipoDocumento,
        string documento,
        string originMethod);

    /// <summary>Avanza el step de la sesión al valor indicado.</summary>
    Task ActualizarStepAsync(string flowId, string nuevoStep);

    /// <summary>Elimina la sesión del flujo de registro.</summary>
    Task EliminarFlowSessionAsync(string flowId);

    /// <summary>
    /// Orquesta la confirmación de una nueva persona: valida, guarda en Redis y envía email.
    /// NO crea la persona en t_persona (se difiere a CompletarPassword).
    /// </summary>
    Task<OperationResult<RegistroFlowResult>> ConfirmarNuevaPersonaAsync(
        DtoRegistroPersonaRequest request,
        string flowId);

    /// <summary>Retorna los datos de una persona pendiente en Redis, o null si no existe/expiró.</summary>
    Task<DtoRegistroPendingPersona?> GetPendingPersonaAsync(string flowId);

    /// <summary>Elimina los datos de persona pendiente en Redis.</summary>
    Task DeletePendingPersonaAsync(string flowId);

    /// <summary>
    /// Crea la persona en DB y establece la contraseña definitiva en LDAP.
    /// Se llama desde CompletarPassword para el flujo de nueva persona.
    /// Retorna el CodigoPersona creado.
    /// </summary>
    Task<OperationResult<long>> CompletarNuevaPersona(DtoRegistroPendingPersona data, string passwordNueva);
}

/// <summary>
/// Resultado de ConfirmarNuevaPersonaAsync. <see cref="MailEnviado"/> distingue éxito parcial
/// (registro OK, mail no enviado) de éxito completo.
/// </summary>
public record RegistroFlowResult(string Message, bool MailEnviado = true);
