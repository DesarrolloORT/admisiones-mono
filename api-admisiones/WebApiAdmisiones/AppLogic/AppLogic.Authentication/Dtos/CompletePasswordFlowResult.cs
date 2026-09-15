using System.Diagnostics.CodeAnalysis;
using Utilities;

namespace AppLogic.Authentication.Dtos;

/// <summary>
/// Resultado de la orquestación de CompleteInitialPassword (persona nueva vía Redis / persona existente).
/// Separa el resultado de negocio de la decisión HTTP de limpiar la cookie temporal de activación,
/// que sigue siendo responsabilidad del controller.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CompletePasswordFlowResult
{
    /// <summary>Respuesta que el controller devuelve al front.</summary>
    public required OperationResult<AuthenticationResponse> Result { get; init; }

    /// <summary>Indica si el controller debe limpiar la cookie temporal X-Password-Activation.</summary>
    public bool ClearActivationCookie { get; init; }
}
