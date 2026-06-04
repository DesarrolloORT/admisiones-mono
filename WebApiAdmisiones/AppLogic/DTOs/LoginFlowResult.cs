using System.Diagnostics.CodeAnalysis;
using Utilities;

namespace AppLogic.DTOs
{
    [ExcludeFromCodeCoverage]
    public sealed class LoginFlowResult
    {
        public OperationResult<DtoAuthenticationResponse>? AuthResult { get; private init; }
        public OperationResult<DtoLogin2FARequired>? TwoFactorResult { get; private init; }
        public bool SetCookies { get; private init; }
        public LoginRateLimitHeaders? RateLimitHeaders { get; private init; }

        public bool RequiresTwoFactor => TwoFactorResult != null;

        public static LoginFlowResult LoginExitoso(OperationResult<DtoAuthenticationResponse> result) =>
            new() { AuthResult = result, SetCookies = true };

        public static LoginFlowResult Requiere2FA(OperationResult<DtoLogin2FARequired> result) =>
            new() { TwoFactorResult = result };

        public static LoginFlowResult Fallo(OperationResult<DtoAuthenticationResponse> result) =>
            new() { AuthResult = result };

        public static LoginFlowResult FalloConRateLimit(
            OperationResult<DtoAuthenticationResponse> result,
            LoginRateLimitHeaders headers) =>
            new() { AuthResult = result, RateLimitHeaders = headers };
    }

    [ExcludeFromCodeCoverage]
    public sealed class LoginRateLimitHeaders
    {
        public required int Limit { get; init; }
        public required int Remaining { get; init; }
        public DateTimeOffset? ResetTime { get; init; }
    }
}
