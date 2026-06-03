using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Utilities;
using WebApiAdmisiones.Extensions;

namespace WebApiAdmisiones.Security
{
    public class LoginFlowService : ILoginFlowService
    {
        private readonly IAuthService _authService;
        private readonly IRedisRateLimiterService _rateLimiter;
        private readonly IRecaptchaService _recaptchaService;
        private readonly IDosFactoresAuthService _dosFactoresService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginFlowService> _logger;

        public LoginFlowService(
            IAuthService authService,
            IRedisRateLimiterService rateLimiter,
            IRecaptchaService recaptchaService,
            IDosFactoresAuthService dosFactoresService,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<LoginFlowService> logger)
        {
            _authService = authService;
            _rateLimiter = rateLimiter;
            _recaptchaService = recaptchaService;
            _dosFactoresService = dosFactoresService;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginFlowResult> EjecutarAsync(AuthRequest request, string ipAddress, string captchaToken)
        {
            // PASO 1: Validar score reCAPTCHA (omitir en entornos locales/dev)
            double recaptchaScore = 1.0;
            if (!_environment.IsDevelopment() && !_environment.IsEnvironment("LocalHost"))
            {
                var captchaResult = await _recaptchaService.ValidarConScoreAsync(captchaToken, "login");
                if (!captchaResult.Success)
                {
                    _logger.LogWarning("Validación reCAPTCHA fallida. Código de error: {Code}", captchaResult.ErrorCode);

                    return LoginFlowResult.Fallo(OperationResult<DtoAuthenticationResponse>.IsFailed(
                        captchaResult.ErrorCode,
                        nameof(EjecutarAsync),
                        captchaResult.Message,
                        captchaResult.HttpCode,
                        default!));
                }

                recaptchaScore = captchaResult.Data;
            }

            // PASO 2: Rate limit por IP + Documento
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
                        $"Se superó el límite de intentos de inicio de sesión para esta cuenta ({maxAccountAttempts} intentos cada {windowMinutes} minutos). Por tu seguridad, intentá nuevamente más tarde.",
                        429,
                        default!),
                    new LoginRateLimitHeaders
                    {
                        Limit = maxAccountAttempts,
                        Remaining = accountRateLimit.RemainingAttempts,
                        ResetTime = accountRateLimit.ResetTime
                    });
            }

            // PASO 3: Verificar contadores de fallo de credenciales (bloqueo post-fallo)
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

            // PASO 4: Intentar autenticación LDAP
            var result = await _authService.AutenticarUsuarioLDAPAsync(request.TipoDocumento, request.Documento, request.Password);

            if (!result.Success || result.Data == null)
            {
                await Task.WhenAll(
                    _rateLimiter.IsAllowedAsync(failUserKey, failUserLimit, failWindow),
                    _rateLimiter.IsAllowedAsync(failIpKey, failIpLimit, failWindow));

                return LoginFlowResult.Fallo(result);
            }

            // PASO 5: Autenticación exitosa — limpiar contadores de fallo
            await Task.WhenAll(
                _rateLimiter.ClearAsync(failUserKey),
                _rateLimiter.ClearAsync(failIpKey));

            // PASO 6: Evaluar score reCAPTCHA para decidir login directo o flujo 2FA
            var minimumScore = ObtenerScoreMinimo();
            if (recaptchaScore > minimumScore)
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

            // PASO 7: Score bajo — iniciar flujo 2FA
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

        private static double ObtenerScoreMinimo()
        {
            var rawValue = Environment.GetEnvironmentVariable("RECAPTCHA_SCORE") ?? "0.5";
            return double.TryParse(
                rawValue,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var score)
                ? score
                : 0.5;
        }
    }
}
