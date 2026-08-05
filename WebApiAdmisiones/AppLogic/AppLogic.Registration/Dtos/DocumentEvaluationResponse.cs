using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.Registration.Dtos;

[ExcludeFromCodeCoverage]
public class DocumentEvaluationResponse
{
    /// <summary>
    /// La persona existe y ya tiene usuario LDAP. El front debe mostrar el mensaje del OperationResult y ofrecer acceso o recuperacion.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UserAlreadyRegistered { get; set; }

    /// <summary>
    /// La persona existe en SGI pero no tiene usuario LDAP. El front debe pedir primer apellido, mail y verificacion de mail.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RequiresIdentityVerification { get; set; }

    /// <summary>
    /// Es una CI valida sin persona existente. El front debe pedir datos completos, incluyendo direccion y ciudad.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RequiresPersonRegistration { get; set; }

    /// <summary>
    /// No existe solicitud de alta para el documento. El front debe pedir datos para crear la solicitud.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RequiresRegistrationRequest { get; set; }

    /// <summary>
    /// Ya existe una solicitud de alta para el documento ingresado.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RegistrationRequestPending { get; set; }

    /// <summary>
    /// Identificador de la sesión de registro. Debe enviarse en el header X-Flow-Id
    /// en todos los pasos subsiguientes del flujo.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? FlowId { get; set; }
}
