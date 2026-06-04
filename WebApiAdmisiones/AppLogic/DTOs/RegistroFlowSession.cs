using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs;

/// <summary>
/// Sesión de registro almacenada en Redis durante el flujo de onboarding.
/// Se crea en EvaluarDocumento y se valida en los pasos subsiguientes vía X-Flow-Id.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RegistroFlowSession
{
    public string FlowId { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;

    /// <summary>Codigo de persona si ya existe en SGI (null si es persona nueva).</summary>
    public long? CodigoPersona { get; set; }

    /// <summary>
    /// Paso actual del flujo.
    /// Valores: "evaluado" | "identidad_verificada" | "confirmado"
    /// </summary>
    public string Step { get; set; } = "evaluado";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
