using System.Diagnostics.CodeAnalysis;
using Utilities;

namespace AppLogic.Authentication.Dtos;

/// <summary>
/// Desenlace del login, con las decisiones HTTP que el controller todavía tiene que aplicar
/// (cookies y headers de rate limit). No sale al front: el controller devuelve uno de los dos
/// <c>OperationResult</c> que trae adentro.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LoginFlowResult
{
    /// <summary>Resultado del login cuando no hubo 2FA (éxito o fallo).</summary>
    public OperationResult<AuthenticationResponse>? AuthResult { get; private init; }

    /// <summary>Resultado cuando el login quedó pendiente del segundo factor.</summary>
    public OperationResult<TwoFactorRequiredResponse>? TwoFactorResult { get; private init; }

    /// <summary>El controller debe escribir las cookies de sesión.</summary>
    public bool SetCookies { get; private init; }

    /// <summary>Headers de rate limit a devolver, cuando el intento se contabilizó.</summary>
    public LoginRateLimitHeaders? RateLimitHeaders { get; private init; }

    /// <summary>El login no terminó: falta verificar el segundo factor.</summary>
    public bool RequiresTwoFactor => TwoFactorResult != null;

    public static LoginFlowResult LoginSucceeded(OperationResult<AuthenticationResponse> result) =>
        new() { AuthResult = result, SetCookies = true };

    public static LoginFlowResult TwoFactorRequired(OperationResult<TwoFactorRequiredResponse> result) =>
        new() { TwoFactorResult = result };

    public static LoginFlowResult Failed(OperationResult<AuthenticationResponse> result) =>
        new() { AuthResult = result };

    public static LoginFlowResult FailedWithRateLimit(
        OperationResult<AuthenticationResponse> result,
        LoginRateLimitHeaders headers) =>
        new() { AuthResult = result, RateLimitHeaders = headers };
}

/// <summary>Headers <c>X-RateLimit-*</c> del intento de login.</summary>
[ExcludeFromCodeCoverage]
public sealed class LoginRateLimitHeaders
{
    /// <summary>Intentos permitidos en la ventana.</summary>
    public required int Limit { get; init; }

    /// <summary>Intentos que quedan.</summary>
    public required int Remaining { get; init; }

    /// <summary>Cuándo se reinicia la ventana.</summary>
    public DateTimeOffset? ResetTime { get; init; }
}
