using System.Diagnostics.CodeAnalysis;
using AppLogic.Registration.Constants;

namespace AppLogic.Registration.Dtos;

/// <summary>
/// Sesión de registro almacenada en Redis durante el flujo de onboarding.
/// Se crea en EvaluateDocument y se valida en los pasos subsiguientes vía X-Flow-Id.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RegistrationFlowSession
{
    /// <summary>Identificador de la sesión, el que viaja en el header X-Flow-Id.</summary>
    public string FlowId { get; set; } = string.Empty;

    /// <summary>Tipo de documento con el que arrancó el flujo.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de documento con el que arrancó el flujo.</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Codigo de persona si ya existe en SGI (null si es persona nueva).</summary>
    public long? PersonId { get; set; }

    /// <summary>
    /// Paso actual del flujo. Ver <see cref="RegistrationFlowConstants.Step"/>.
    /// </summary>
    public string Step { get; set; } = RegistrationFlowConstants.Step.Evaluado;

    /// <summary>Momento de creación de la sesión, en UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
