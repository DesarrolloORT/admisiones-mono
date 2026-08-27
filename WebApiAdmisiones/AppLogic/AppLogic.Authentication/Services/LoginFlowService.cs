using AppLogic.Contracts.Text;
using AppLogic.Authentication.Dtos;
using AppLogic.Platform.RateLimiting;
using AppLogic.Authentication.Contracts;
using AppLogic.Authentication.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppLogic.Contracts;
using Utilities;

namespace AppLogic.Authentication.Services;

public class LoginFlowService : ILoginFlowService
{
    private readonly IAuthenticateWithLdap _authenticateWithLdap;
    private readonly IIssueTokensForPerson _issueTokensForPerson;
    private readonly IRateLimiterService _rateLimiter;
    private readonly ITwoFactorAuthService _dosFactoresService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoginFlowService> _logger;

    public LoginFlowService(
        IAuthenticateWithLdap authenticateWithLdap,
        IIssueTokensForPerson issueTokensForPerson,
        IRateLimiterService rateLimiter,
        ITwoFactorAuthService dosFactoresService,
        IConfiguration configuration,
        ILogger<LoginFlowService> logger)
    {
        _authenticateWithLdap = authenticateWithLdap;
        _issueTokensForPerson = issueTokensForPerson;
        _rateLimiter = rateLimiter;
        _dosFactoresService = dosFactoresService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginFlowResult> ExecuteAsync(AuthRequest request, string ipAddress, double recaptchaScore)
    {
        var maxAccountAttempts = _configuration.GetValue<int?>("Authentication:Login:RateLimitAccountAttempts") ?? 5;
        var windowMinutes = _configuration.GetValue<int?>("Authentication:Login:RateLimitWindowMinutes") ?? 15;

        var accountRateLimit = await _rateLimiter.ValidateAsync(
            ipAddress,
            request.DocumentType,
            request.DocumentNumber,
            maxAccountAttempts,
            TimeSpan.FromMinutes(windowMinutes));

        if (!accountRateLimit.IsAllowed)
        {
            _logger.LogWarning(
                "Límite de intentos SUPERADO para la cuenta {TipoDoc}:{Doc} desde IP {IP}. " +
                "Intentos: {Remaining}/{Limit}. Partición: {Key}",
                request.DocumentType,
                request.DocumentNumber,
                ipAddress,
                accountRateLimit.RemainingAttempts,
                maxAccountAttempts,
                accountRateLimit.PartitionKey);

            return LoginFlowResult.FailedWithRateLimit(
                OperationResult<AuthenticationResponse>.IsFailed(
                    "AUTH_RL_02",
                    nameof(ExecuteAsync),
                    $"Se superó el límite de intentos de inicio de sesión para esta cuenta. Por tu seguridad, intentá nuevamente más tarde.",
                    429,
                    default!),
                new LoginRateLimitHeaders
                {
                    Limit = maxAccountAttempts,
                    Remaining = accountRateLimit.RemainingAttempts,
                    ResetTime = accountRateLimit.ResetTime
                });
        }

        var failUserLimit = _configuration.GetValue<int?>("Authentication:Login:FailedAttemptLimitUser") ?? 5;
        var failIpLimit = _configuration.GetValue<int?>("Authentication:Login:FailedAttemptLimitIp") ?? 10;
        var failWindowMinutes = _configuration.GetValue<int?>("Authentication:Login:FailedAttemptWindowMinutes") ?? 15;
        var failWindow = TimeSpan.FromMinutes(failWindowMinutes);

        var normalizedDoc = TextNormalization.NormalizeDocumentForKey(request.DocumentNumber);
        var failUserKey = $"login-fail-cred-user:{normalizedDoc}";
        var failIpKey = $"login-fail-cred-ip:{ipAddress}";

        var userFailRemaining = await _rateLimiter.GetRemainingAsync(failUserKey, failUserLimit, failWindow);
        if (userFailRemaining == 0)
        {
            _logger.LogWarning("Bloqueo por intentos fallidos activo para el documento {Doc}", normalizedDoc);

            return LoginFlowResult.Failed(OperationResult<AuthenticationResponse>.IsFailed(
                "AUTH_RL_03",
                nameof(ExecuteAsync),
                $"Se bloqueó el acceso temporalmente por múltiples intentos fallidos. Intentá nuevamente en {failWindowMinutes} minutos.",
                429,
                default!));
        }

        var ipFailRemaining = await _rateLimiter.GetRemainingAsync(failIpKey, failIpLimit, failWindow);
        if (ipFailRemaining == 0)
        {
            _logger.LogWarning("Bloqueo por intentos fallidos activo para la IP {IP}", ipAddress);

            return LoginFlowResult.Failed(OperationResult<AuthenticationResponse>.IsFailed(
                "AUTH_RL_04",
                nameof(ExecuteAsync),
                $"Se bloqueó el acceso temporalmente por múltiples intentos fallidos desde esta red. Intentá nuevamente en {failWindowMinutes} minutos.",
                429,
                default!));
        }

        var result = await _authenticateWithLdap.ExecuteAsync(
            request.DocumentType!,
            request.DocumentNumber!,
            request.Password);

        if (!result.Success || result.Data == null)
        {
            await Task.WhenAll(
                _rateLimiter.IsAllowedAsync(failUserKey, failUserLimit, failWindow),
                _rateLimiter.IsAllowedAsync(failIpKey, failIpLimit, failWindow));

            return LoginFlowResult.Failed(result.Failure().As<AuthenticationResponse>(nameof(ExecuteAsync)));
        }

        await Task.WhenAll(
            _rateLimiter.ClearAsync(failUserKey),
            _rateLimiter.ClearAsync(failIpKey));

        var person = result.Data;
        var minimumScore = _configuration.GetValue<double>("RECAPTCHA_SCORE", 0.5);

        if (recaptchaScore > minimumScore)
        {
            // Sin 2FA pendiente: recién acá se emiten y persisten los tokens.
            var tokenResult = await _issueTokensForPerson.ExecuteAsync(person.PersonId);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Persona {PersonId} autenticada exitosamente (score: {Score})",
                    person.PersonId,
                    recaptchaScore);
            }

            return LoginFlowResult.LoginSucceeded(tokenResult);
        }

        if (string.IsNullOrWhiteSpace(person.Email))
        {
            _logger.LogWarning(
                "Score reCAPTCHA bajo ({Score}) para la persona {PersonId} pero no tiene email registrado. Acceso denegado.",
                recaptchaScore,
                person.PersonId);

            return LoginFlowResult.Failed(OperationResult<AuthenticationResponse>.IsFailed(
                "AUTH_2FA_NO_EMAIL",
                nameof(ExecuteAsync),
                "No es posible verificar tu identidad por este medio. Contactá a soporte.",
                422,
                default!));
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Score reCAPTCHA bajo ({Score}) para la persona {PersonId}. Iniciando 2FA.",
                recaptchaScore,
                person.PersonId);
        }

        // Todavía no hay tokens: la sesión 2FA solo guarda identidad verificada.
        var twoFactorResult = await _dosFactoresService.StartAsync(person, person.Email);
        if (!twoFactorResult.Success)
        {
            return LoginFlowResult.Failed(twoFactorResult.Failure().As<AuthenticationResponse>(nameof(ExecuteAsync)));
        }

        return LoginFlowResult.TwoFactorRequired(
            OperationResult<TwoFactorRequiredResponse>.IsSuccess(
                twoFactorResult.Data!,
                nameof(ExecuteAsync),
                twoFactorResult.Message,
                202));
    }
}
