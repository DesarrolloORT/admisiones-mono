using AppLogic.Identity.Dtos;
using AppLogic.Contracts.Text;
using AppLogic.Authentication.Dtos;
using AppLogic.Authentication.Rules;
using AppLogic.Platform.Email;
using AppLogic.Platform.RateLimiting;
using AppLogic.Authentication.Contracts;
using AppLogic.Authentication.Interfaces;
using MailORT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Utilities;

namespace AppLogic.Authentication.Services;

/// <summary>
/// Implementación del servicio de autenticación de dos factores.
/// Almacena sesiones temporales en Redis y envía códigos de verificación por email.
/// </summary>
public class TwoFactorAuthService : ITwoFactorAuthService
{
    private const int DefaultSessionMinutes = 30;
    private const int DefaultCodeMinutes = 10;

    private readonly ITwoFactorSessionStore _sessionStore;
    private readonly IRateLimiterService _rateLimiter;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwoFactorAuthService> _logger;
    private readonly IIssueTokensForPerson _issueTokensForPerson;

    public TwoFactorAuthService(
        ITwoFactorSessionStore sessionStore,
        IRateLimiterService rateLimiter,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<TwoFactorAuthService> logger,
        IIssueTokensForPerson issueTokensForPerson)
    {
        _sessionStore = sessionStore;
        _rateLimiter = rateLimiter;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
        _issueTokensForPerson = issueTokensForPerson;
    }

    public async Task<OperationResult<TwoFactorRequiredResponse>> StartAsync(
        AuthenticatedPerson person,
        string email)
    {
        try
        {
            var normalizedDoc = TextNormalization.NormalizeDocumentForKey(person.DocumentNumber);
            var initLimit = _configuration.GetValue<int?>("Authentication:TwoFactor:MaxInitAttempts") ?? 3;
            var sessionMinutes = GetSessionMinutes();
            var codeMinutes = GetCodeMinutes();
            var initWindow = TimeSpan.FromMinutes(sessionMinutes);

            var allowed = await _rateLimiter.IsAllowedAsync(
                $"2fa-init:{normalizedDoc}",
                initLimit,
                initWindow);

            if (!allowed)
            {
                _logger.LogWarning(
                    "Límite de solicitudes de inicio 2FA superado para el documento {Doc}",
                    normalizedDoc);

                return OperationResult<TwoFactorRequiredResponse>.IsFailed(
                    "AUTH_2FA_INIT_01",
                    nameof(StartAsync),
                    $"Se superó el límite de solicitudes de verificación para esta cuenta. Intentá nuevamente en {sessionMinutes} minutos.",
                    429,
                    default!);
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var codeLength = _configuration.GetValue<int?>("Authentication:TwoFactor:CodeLength") ?? 6;
            var code = GenerateCode(codeLength);
            var codeHash = HashCode(code);
            var codeExpiresAtUtc = DateTime.UtcNow.AddMinutes(codeMinutes);

            var session = new TwoFactorSession
            {
                PersonId = person.PersonId,
                FirstName = person.FirstName,
                MiddleName = person.MiddleName,
                FirstSurname = person.FirstSurname,
                SecondSurname = person.SecondSurname,
                PersonType = person.PersonType,
                DocumentNumber = person.DocumentNumber,
                CodeHash = codeHash,
                CodeExpiresAtUtc = codeExpiresAtUtc,
                Email = email,
                Attempts = 0
            };

            await _sessionStore.SaveAsync(sessionId, session, TimeSpan.FromMinutes(sessionMinutes));

            // Enviar código por email. Si falla, eliminar la sesión.
            try
            {
                await SendCodeMailAsync(email, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email 2FA a {Email}", email);
                await _sessionStore.DeleteAsync(sessionId);

                return OperationResult<TwoFactorRequiredResponse>.IsFailed(
                    "AUTH_2FA_MAIL_01",
                    nameof(StartAsync),
                    "No fue posible enviar el código de verificación. Intentá nuevamente.",
                    500,
                    default!);
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "2FA iniciado para persona {CodigoPersona}, sesión {SessionId}",
                    person.PersonId,
                    sessionId);
            }

            return OperationResult<TwoFactorRequiredResponse>.Ok(
                new TwoFactorRequiredResponse
                {
                    SessionId = sessionId,
                    MaskedEmail = EmailMasking.Mask(email)
                },
                nameof(StartAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al iniciar 2FA");

            return OperationResult<TwoFactorRequiredResponse>.IsFailed(
                "AUTH_2FA_INIT_99",
                nameof(StartAsync),
                "Error al iniciar la verificación de identidad.",
                500,
                default!);
        }
    }

    public async Task<OperationResult<AuthenticationResponse>> VerifyCodeAsync(
        string sessionId,
        string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(code))
            {
                return OperationResult<AuthenticationResponse>.IsFailed(
                    "AUTH_2FA_01",
                    nameof(VerifyCodeAsync),
                    "La sesión y el código son requeridos.",
                    400,
                    default!);
            }

            var session = await _sessionStore.GetAsync(sessionId);

            if (session == null)
            {
                return OperationResult<AuthenticationResponse>.IsFailed(
                    "AUTH_2FA_02",
                    nameof(VerifyCodeAsync),
                    "La sesión de verificación no existe o ha expirado.",
                    401,
                    default!);
            }

            var maxAttempts = _configuration.GetValue<int?>("Authentication:TwoFactor:MaxCodeAttempts") ?? 5;

            if (CodeExpired(session))
            {
                return OperationResult<AuthenticationResponse>.IsFailed(
                    "AUTH_2FA_06",
                    nameof(VerifyCodeAsync),
                    "El código de verificación expiró. Solicitá uno nuevo para continuar.",
                    401,
                    default!);
            }

            var codeHash = HashCode(code.Trim());
            if (!string.Equals(codeHash, session.CodeHash, StringComparison.Ordinal))
            {
                session.Attempts++;

                if (session.Attempts >= maxAttempts)
                {
                    await _sessionStore.DeleteAsync(sessionId);

                    _logger.LogWarning(
                        "Máximo de intentos 2FA superado para la sesión {SessionId}, persona {CodigoPersona}",
                        sessionId,
                        session.PersonId);

                    return OperationResult<AuthenticationResponse>.IsFailed(
                        "AUTH_2FA_04",
                        nameof(VerifyCodeAsync),
                        "Se superó el máximo de intentos de verificación. Por tu seguridad, iniciá el proceso nuevamente.",
                        401,
                        default!);
                }

                var ttl = await _sessionStore.GetTtlAsync(sessionId) ?? TimeSpan.FromMinutes(GetSessionMinutes());
                await _sessionStore.UpdateAsync(sessionId, session, ttl);

                var remaining = maxAttempts - session.Attempts;

                return OperationResult<AuthenticationResponse>.IsFailed(
                    "AUTH_2FA_05",
                    nameof(VerifyCodeAsync),
                    $"Código incorrecto. Te quedan {remaining} intento(s).",
                    401,
                    default!);
            }

            // Código correcto: eliminar sesión y recién ahora emitir y persistir los tokens.
            await _sessionStore.DeleteAsync(sessionId);
            await ClearLoginRateLimitAsync(session);

            var tokenResult = await _issueTokensForPerson.ExecuteAsync(
                session.PersonId,
                "Verificación completada. Los tokens han sido establecidos como cookies seguras.");

            if (!tokenResult.Success)
            {
                return tokenResult;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "2FA verificado exitosamente para la persona {CodigoPersona}",
                    session.PersonId);
            }

            return OperationResult<AuthenticationResponse>.Ok(tokenResult.Data!, nameof(VerifyCodeAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al verificar el código 2FA para la sesión {SessionId}", sessionId);

            return OperationResult<AuthenticationResponse>.IsFailed(
                "AUTH_2FA_99",
                nameof(VerifyCodeAsync),
                "Error al verificar el código.",
                500,
                default!);
        }
    }

    public async Task<OperationResult<TwoFactorRequiredResponse>> ResendCodeAsync(string sessionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return OperationResult<TwoFactorRequiredResponse>.IsFailed(
                    "AUTH_2FA_RESEND_01",
                    nameof(ResendCodeAsync),
                    "La sesión es requerida.",
                    400,
                    default!);
            }

            var session = await _sessionStore.GetAsync(sessionId);

            if (session == null)
            {
                return OperationResult<TwoFactorRequiredResponse>.IsFailed(
                    "AUTH_2FA_02",
                    nameof(ResendCodeAsync),
                    "La sesión de verificación no existe o ha expirado.",
                    401,
                    default!);
            }

            if (string.IsNullOrWhiteSpace(session.Email))
            {
                return OperationResult<TwoFactorRequiredResponse>.IsFailed(
                    "AUTH_2FA_RESEND_03",
                    nameof(ResendCodeAsync),
                    "La sesión no tiene un email válido para reenviar el código.",
                    422,
                    default!);
            }

            var sessionMinutes = GetSessionMinutes();
            var initLimit = _configuration.GetValue<int?>("Authentication:TwoFactor:MaxInitAttempts") ?? 3;
            var normalizedDoc = TextNormalization.NormalizeDocumentForKey(session.DocumentNumber);
            var allowed = await _rateLimiter.IsAllowedAsync(
                $"2fa-init:{normalizedDoc}",
                initLimit,
                TimeSpan.FromMinutes(sessionMinutes));

            if (!allowed)
            {
                _logger.LogWarning(
                    "Límite de reenvíos 2FA superado para el documento {Doc}",
                    normalizedDoc);

                return OperationResult<TwoFactorRequiredResponse>.IsFailed(
                    "AUTH_2FA_RESEND_04",
                    nameof(ResendCodeAsync),
                    $"Se superó el límite de solicitudes de verificación para esta cuenta. Intentá nuevamente en {sessionMinutes} minutos.",
                    429,
                    default!);
            }

            var ttl = await _sessionStore.GetTtlAsync(sessionId) ?? TimeSpan.FromMinutes(sessionMinutes);
            var codeLength = _configuration.GetValue<int?>("Authentication:TwoFactor:CodeLength") ?? 6;
            var codeMinutes = GetCodeMinutes();
            var code = GenerateCode(codeLength);

            try
            {
                await SendCodeMailAsync(session.Email, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reenviar email 2FA a {Email}", session.Email);

                return OperationResult<TwoFactorRequiredResponse>.IsFailed(
                    "AUTH_2FA_RESEND_MAIL_01",
                    nameof(ResendCodeAsync),
                    "No fue posible reenviar el código de verificación. Intentá nuevamente.",
                    500,
                    default!);
            }

            session.CodeHash = HashCode(code);
            session.CodeExpiresAtUtc = DateTime.UtcNow.AddMinutes(codeMinutes);
            session.Attempts = 0;

            await _sessionStore.UpdateAsync(sessionId, session, ttl);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "2FA reenviado para persona {CodigoPersona}, sesión {SessionId}",
                    session.PersonId,
                    sessionId);
            }

            return OperationResult<TwoFactorRequiredResponse>.Ok(
                new TwoFactorRequiredResponse
                {
                    SessionId = sessionId,
                    MaskedEmail = EmailMasking.Mask(session.Email),
                    Message = "Se reenvió un nuevo código de verificación a tu correo electrónico."
                },
                nameof(ResendCodeAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al reenviar el código 2FA para la sesión {SessionId}", sessionId);

            return OperationResult<TwoFactorRequiredResponse>.IsFailed(
                "AUTH_2FA_RESEND_99",
                nameof(ResendCodeAsync),
                "Error al reenviar el código.",
                500,
                default!);
        }
    }

    private Task SendCodeMailAsync(string email, string code)
    {
        var body = BuildInstitutionalMailBody(code);
        return _emailSender.SendAsync(email, "Tu código de verificación - Admisiones ORT", body);
    }

    private static string BuildInstitutionalMailBody(string code) =>
        $"""
        {EnvioMail.CabezalHTML()}
        {EnvioMail.Cabezal()}
        {EnvioMail.CuerpoConTitulo("Verificaci&oacute;n de identidad", DateTime.Now, "Estimado/a:")}
        {EnvioMail.CuerpoParrafos($"Recibimos una solicitud para ingresar al sitio de Admisiones. Para completar el inicio de sesi&oacute;n, ingres&aacute; el siguiente c&oacute;digo de verificaci&oacute;n:<br><br><strong style=\"font-size: 24px; letter-spacing: 6px;\">{code}</strong>")}
        {EnvioMail.CuerpoParrafos("Por seguridad, este c&oacute;digo vence en breve y puede usarse una sola vez. Si no realizaste esta solicitud, pod&eacute;s ignorar este mensaje.")}
        {EnvioMail.CuerpoParrafos("ORT nunca te solicitar&aacute; actualizar tu usuario, contrase&ntilde;a o datos de medios de pago electr&oacute;nicos por e-mail, tel&eacute;fono, SMS, WhatsApp ni redes sociales. M&aacute;s informaci&oacute;n en: <a href=\"https://www.ort.edu.uy/ciberseguridad\" target=\"_blank\" rel=\"noopener noreferrer\">www.ort.edu.uy/ciberseguridad</a>.")}
        {EnvioMail.CuerpoParrafos("Atentamente,<br>Departamento de Admisiones")}
        {EnvioMail.FinHtml}
        """;

    private static string GenerateCode(int length)
    {
        var digits = new char[length];
        for (var i = 0; i < length; i++)
            digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        return new string(digits);
    }

    private static string HashCode(string codigo)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codigo));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private int GetSessionMinutes() =>
        _configuration.GetValue<int?>("Authentication:TwoFactor:SessionMinutes") ?? DefaultSessionMinutes;

    private int GetCodeMinutes() =>
        _configuration.GetValue<int?>("Authentication:TwoFactor:CodeMinutes") ?? DefaultCodeMinutes;

    private static bool CodeExpired(TwoFactorSession session) =>
        session.CodeExpiresAtUtc <= DateTime.UtcNow;

    private async Task ClearLoginRateLimitAsync(TwoFactorSession session)
    {
        try
        {
            await _rateLimiter.ClearAsync($"2fa-init:{TextNormalization.NormalizeDocumentForKey(session.DocumentNumber)}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo limpiar el rate limit de inicio 2FA para la person {CodigoPersona}.",
                session.PersonId);
        }
    }
}
