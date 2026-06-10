using AppLogic.DTOs;
using AppLogic.IServices.Autenticacion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Utilities;
using WebApiAdmisiones.Security.Captcha;
using WebApiAdmisiones.Security.RateLimiting;

namespace WebApiAdmisiones.Security.Authentication
{
    public class LoginFlowService : ILoginFlowService
    {
        private readonly IAuthService _authService;
        private readonly IRedisRateLimiterService _rateLimiter;
        private readonly IDosFactoresAuthService _dosFactoresService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginFlowService> _logger;

        public LoginFlowService(
            IAuthService authService,
            IRedisRateLimiterService rateLimiter,
            IDosFactoresAuthService dosFactoresService,
            IConfiguration configuration,
            ILogger<LoginFlowService> logger)
        {
            _authService = authService;
            _rateLimiter = rateLimiter;
            _dosFactoresService = dosFactoresService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginFlowResult> EjecutarAsync(AuthRequest request, string ipAddress, double recaptchaScore)
        {
            var maxAccountAttempts = _configuration.GetValue<int?>("Authentication:Login:RateLimitAccountAttempts") ?? 5;
            var windowMinutes = _configuration.GetValue<int?>("Authentication:Login:RateLimitWindowMinutes") ?? 15;

            var accountRateLimit = await _rateLimiter.ValidateAsync(
                ipAddress,
                request.TipoDocumento,
                request.Documento,
                maxAccountAttempts,
                TimeSpan.FromMinutes(windowMinutes));

            if (!accountRateLimit.IsAllowed)
            {
                Extensions.ServiceCollectionExtensions.LoginAccountRateLimitRejections.Inc();

                _logger.LogWarning(
                    "Límite de intentos SUPERADO para la cuenta {TipoDoc}:{Doc} desde IP {IP}. " +
                    "Intentos: {Remaining}/{Limit}. Partición: {Key}",
                    request.TipoDocumento,
                    request.Documento,
                    ipAddress,
                    accountRateLimit.RemainingAttempts,
                    maxAccountAttempts,
                    accountRateLimit.PartitionKey);

                return LoginFlowResult.FalloConRateLimit(
                    OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "AUTH_RL_02",
                        nameof(EjecutarAsync),
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

            var normalizedDoc = request.Documento
                .Replace(".", "").Replace("-", "").Replace(" ", "")
                .Trim().ToLowerInvariant();
            var failUserKey = $"login-fail-cred-user:{normalizedDoc}";
            var failIpKey = $"login-fail-cred-ip:{ipAddress}";

            var userFailRemaining = await _rateLimiter.GetRemainingAsync(failUserKey, failUserLimit, failWindow);
            if (userFailRemaining == 0)
            {
                _logger.LogWarning("Bloqueo por intentos fallidos activo para el documento {Doc}", normalizedDoc);

                return LoginFlowResult.Fallo(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "AUTH_RL_03",
                    nameof(EjecutarAsync),
                    $"Se bloqueó el acceso temporalmente por múltiples intentos fallidos. Intentá nuevamente en {failWindowMinutes} minutos.",
                    429,
                    default!));
            }

            var ipFailRemaining = await _rateLimiter.GetRemainingAsync(failIpKey, failIpLimit, failWindow);
            if (ipFailRemaining == 0)
            {
                _logger.LogWarning("Bloqueo por intentos fallidos activo para la IP {IP}", ipAddress);

                return LoginFlowResult.Fallo(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "AUTH_RL_04",
                    nameof(EjecutarAsync),
                    $"Se bloqueó el acceso temporalmente por múltiples intentos fallidos desde esta red. Intentá nuevamente en {failWindowMinutes} minutos.",
                    429,
                    default!));
            }

            var result = await _authService.AutenticarUsuarioLDAPAsync(
                request.TipoDocumento,
                request.Documento,
                request.Password);

            if (!result.Success || result.Data == null)
            {
                await Task.WhenAll(
                    _rateLimiter.IsAllowedAsync(failUserKey, failUserLimit, failWindow),
                    _rateLimiter.IsAllowedAsync(failIpKey, failIpLimit, failWindow));

                return LoginFlowResult.Fallo(result);
            }

            await Task.WhenAll(
                _rateLimiter.ClearAsync(failUserKey),
                _rateLimiter.ClearAsync(failIpKey));

            if (recaptchaScore > RecaptchaSettings.MinimumScore)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Usuario {Doc} autenticado exitosamente (score: {Score})",
                        request.Documento,
                        recaptchaScore);
                }

                return LoginFlowResult.LoginExitoso(result);
            }

            if (string.IsNullOrWhiteSpace(result.Data.Persona.Email))
            {
                _logger.LogWarning(
                    "Score reCAPTCHA bajo ({Score}) para {Doc} pero no tiene email registrado. Acceso denegado.",
                    recaptchaScore,
                    request.Documento);

                return LoginFlowResult.Fallo(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "AUTH_2FA_NO_EMAIL",
                    nameof(EjecutarAsync),
                    "No es posible verificar tu identidad por este medio. Contactá a soporte.",
                    422,
                    default!));
            }

            _logger.LogInformation(
                "Score reCAPTCHA bajo ({Score}) para {Doc}. Iniciando 2FA.",
                recaptchaScore,
                request.Documento);

            var twoFactorResult = await _dosFactoresService.IniciarAsync(result.Data, result.Data.Persona.Email);
            if (!twoFactorResult.Success)
            {
                return LoginFlowResult.Fallo(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    twoFactorResult.ErrorCode,
                    nameof(EjecutarAsync),
                    twoFactorResult.Message,
                    twoFactorResult.HttpCode,
                    default!));
            }

            return LoginFlowResult.Requiere2FA(
                OperationResult<DtoLogin2FARequired>.Ok(twoFactorResult.Data!, nameof(EjecutarAsync)));
        }
    }
}
