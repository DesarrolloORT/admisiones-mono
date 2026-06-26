using System.Diagnostics.CodeAnalysis;
using Utilities;

namespace AppLogic.Dtos.Autenticacion
{
    [ExcludeFromCodeCoverage]
    public sealed class DtoLoginFlowResult
    {
        public OperationResult<DtoAuthenticationResponse>? AuthResult { get; private init; }
        public OperationResult<DtoLogin2FARequired>? TwoFactorResult { get; private init; }
        public bool SetCookies { get; private init; }
        public DtoLoginRateLimitHeaders? RateLimitHeaders { get; private init; }

        public bool RequiresTwoFactor => TwoFactorResult != null;

        public static DtoLoginFlowResult LoginExitoso(OperationResult<DtoAuthenticationResponse> result) =>
            new() { AuthResult = result, SetCookies = true };

        public static DtoLoginFlowResult Requiere2FA(OperationResult<DtoLogin2FARequired> result) =>
            new() { TwoFactorResult = result };

        public static DtoLoginFlowResult Fallo(OperationResult<DtoAuthenticationResponse> result) =>
            new() { AuthResult = result };

        public static DtoLoginFlowResult FalloConRateLimit(
            OperationResult<DtoAuthenticationResponse> result,
            DtoLoginRateLimitHeaders headers) =>
            new() { AuthResult = result, RateLimitHeaders = headers };
    }

    [ExcludeFromCodeCoverage]
    public sealed class DtoLoginRateLimitHeaders
    {
        public required int Limit { get; init; }
        public required int Remaining { get; init; }
        public DateTimeOffset? ResetTime { get; init; }
    }
}
