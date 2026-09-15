using AppLogic.Identity.Dtos;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Interfaces;
using Utilities;

namespace AppLogic.Registration.Contracts;

/// <summary>
/// Resuelve qué camino sigue el registro para el documento ingresado: alta de persona,
/// verificación de identidad, solicitud de alta o usuario ya registrado.
/// </summary>
public interface IEvaluateDocument
{
    Task<OperationResult<DocumentEvaluationResponse>> ExecuteAsync(EvaluateDocumentRequest request);
}

/// <summary>
/// Registra a una persona que ya existe en el padrón: valida apellido y mail, crea el usuario
/// LDAP, deja el alta de admisión y envía el mail de activación de contraseña. Solo cédula.
/// </summary>
public interface IVerifyIdentity
{
    Task<OperationResult<RegistrationConfirmationResponse?>> ExecuteAsync(VerifyIdentityRequest request);
}

/// <summary>
/// Solo ejecuta las validaciones del alta de persona nueva, sin crear nada en DB ni en LDAP.
/// Lo usa <see cref="Interfaces.IRegistrationFlowService"/> antes de almacenar en Redis.
/// </summary>
public interface IValidateNewPerson
{
    Task<OperationResult<object?>> ExecuteAsync(RegisterPersonRequest request);
}

/// <summary>
/// Completa el alta de una persona nueva con los datos guardados en Redis: crea t_persona,
/// el usuario LDAP con la contraseña definitiva y el registro de admisión.
/// Devuelve el CodigoPersona recién creado.
/// </summary>
public interface ICompleteNewPerson
{
    Task<OperationResult<long>> ExecuteAsync(
        PendingPerson data,
        string newPassword,
        TemporaryDocumentImages? images = null);
}

/// <summary>
/// Deja registrada una solicitud de alta para documentos distintos a cédula de identidad,
/// que después revisa admisiones a mano. Devuelve el mismo tipo que <c>confirm-new-person</c>,
/// con <c>PendingReview = true</c> para que el front distinga los dos finales del registro.
/// </summary>
public interface IConfirmRegistrationRequest
{
    Task<OperationResult<RegistrationFlowResult>> ExecuteAsync(RegisterPersonRequest request);
}
