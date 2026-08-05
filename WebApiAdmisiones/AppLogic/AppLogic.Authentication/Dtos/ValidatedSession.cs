using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Authentication.Dtos;

/// <summary>
/// Resultado de ValidateSessionToken: identifica si la sesión corresponde
/// a una persona ya existente en DB o a una nueva persona pendiente en Redis.
/// </summary>
[ExcludeFromCodeCoverage]
public class ValidatedSession
{
    /// <summary>
    /// Código de persona en DB (solo para flujo de persona existente:
    /// purpose = "password-activation-session").
    /// </summary>
    public long? PersonId { get; set; }

    /// <summary>
    /// FlowId del flujo de nueva persona en Redis (solo para
    /// purpose = "nueva-persona-session").
    /// </summary>
    public string? FlowId { get; set; }

    /// <summary>Propósito del token de sesión ("password-activation-session" | "nueva-persona-session").</summary>
    public string Purpose { get; set; } = string.Empty;
}
