using System.Diagnostics.CodeAnalysis;
using AppLogic.Autenticacion.Responses;
using Utilities;

namespace AppLogic.Autenticacion.Dtos;

/// <summary>
/// Resultado de la orquestación de CompletarPassword (persona nueva vía Redis / persona existente).
/// Separa el resultado de negocio de la decisión HTTP de limpiar la cookie temporal de activación,
/// que sigue siendo responsabilidad del controller.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DtoCompletarPasswordFlowResult
{
    public required OperationResult<DtoAuthenticationResponse> Result { get; init; }

    /// <summary>Indica si el controller debe limpiar la cookie temporal X-Password-Activation.</summary>
    public bool ClearActivationCookie { get; init; }
}
