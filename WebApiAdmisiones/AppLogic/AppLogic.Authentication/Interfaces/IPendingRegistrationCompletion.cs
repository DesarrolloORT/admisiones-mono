using AppLogic.Identity.Dtos;
using Utilities;

namespace AppLogic.Authentication.Interfaces;

/// <summary>
/// Lo único que la autenticación necesita del registro cuando alguien fija su password inicial:
/// leer la persona pendiente, crearla y descartar el flujo.
/// <para>
/// La interfaz vive en Authentication y la implementa Registration (inversión de dependencia).
/// Sin esto, Authentication tendría que referenciar Registration, que ya referencia Authentication:
/// era el último ciclo entre módulos de AppLogic.
/// </para>
/// </summary>
public interface IPendingRegistrationCompletion
{
    /// <summary>Datos de la persona pendiente en Redis, o null si no existe o expiró.</summary>
    Task<PendingPerson?> GetPendingPersonAsync(string flowId);

    /// <summary>
    /// Crea la persona en DB y establece la contraseña definitiva en LDAP.
    /// Devuelve el CodigoPersona creado.
    /// </summary>
    Task<OperationResult<long>> CreatePersonFromPendingAsync(PendingPerson data, string newPassword);

    /// <summary>Elimina los datos de persona pendiente en Redis.</summary>
    Task DeletePendingPersonAsync(string flowId);

    /// <summary>Elimina la sesión del flujo de registro.</summary>
    Task DeleteFlowSessionAsync(string flowId);
}
