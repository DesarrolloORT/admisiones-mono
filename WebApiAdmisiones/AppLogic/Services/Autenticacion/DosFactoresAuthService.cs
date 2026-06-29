using AppLogic.Dtos.Autenticacion;
using AppLogic.Helpers;
using AppLogic.IServices;
using AppLogic.IServices.Autenticacion;
using AppLogic.Utilities;
using MailORT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Utilities;

namespace AppLogic.Services.Autenticacion;

/// <summary>
/// Implementación del servicio de autenticación de dos factores.
/// Almacena sesiones temporales en Redis y envía códigos de verificación por email.
/// </summary>
public class DosFactoresAuthService : IDosFactoresAuthService
{
    private const int DefaultSessionMinutes = 30;
    private const int DefaultCodeMinutes = 10;

    private readonly ITwoFactorSessionStore _sessionStore;
    private readonly IRateLimiterService _rateLimiter;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DosFactoresAuthService> _logger;

    public DosFactoresAuthService(
        ITwoFactorSessionStore sessionStore,
        IRateLimiterService rateLimiter,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<DosFactoresAuthService> logger)
    {
        _sessionStore = sessionStore;
        _rateLimiter = rateLimiter;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OperationResult<DtoLogin2FARequired>> IniciarAsync(
        DtoAuthenticationResponse pendingAuth,
        string email)
    {
        try
        {
            var normalizedDoc = DocumentUtils.NormalizarDocumentoParaClave(pendingAuth.Persona.Documento);
            var initLimit = _configuration.GetValue<int?>("Authentication:TwoFactor:MaxInitAttempts") ?? 3;
            var sessionMinutes = ObtenerSessionMinutes();
            var codeMinutes = ObtenerCodeMinutes();
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

                return OperationResult<DtoLogin2FARequired>.IsFailed(
                    "AUTH_2FA_INIT_01",
                    nameof(IniciarAsync),
                    $"Se superó el límite de solicitudes de verificación para esta cuenta. Intentá nuevamente en {sessionMinutes} minutos.",
                    429,
                    default!);
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var codeLength = _configuration.GetValue<int?>("Authentication:TwoFactor:CodeLength") ?? 6;
            var codigo = GenerarCodigo(codeLength);
            var codigoHash = HashCodigo(codigo);
            var codigoExpiresAtUtc = DateTime.UtcNow.AddMinutes(codeMinutes);

            var session = new DtoTwoFactorSession
            {
                CodigoPersona = pendingAuth.Persona.CodigoPersona,
                PrimerNombre = pendingAuth.Persona.PrimerNombre,
                SegundoNombre = pendingAuth.Persona.SegundoNombre,
                PrimerApellido = pendingAuth.Persona.PrimerApellido,
                SegundoApellido = pendingAuth.Persona.SegundoApellido,
                TipoPersona = pendingAuth.Persona.TipoPersona,
                Documento = pendingAuth.Persona.Documento,
                AccessToken = pendingAuth.AccessToken,
                RefreshToken = pendingAuth.RefreshToken,
                RefreshTokenHash = pendingAuth.RefreshTokenHash,
                CodigoHash = codigoHash,
                CodigoExpiresAtUtc = codigoExpiresAtUtc,
                Email = email,
                Intentos = 0
            };

            await _sessionStore.SaveAsync(sessionId, session, TimeSpan.FromMinutes(sessionMinutes));

            // Enviar código por email. Si falla, eliminar la sesión.
            try
            {
                await EnviarCodigoMailAsync(email, codigo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email 2FA a {Email}", email);
                await _sessionStore.DeleteAsync(sessionId);

                return OperationResult<DtoLogin2FARequired>.IsFailed(
                    "AUTH_2FA_MAIL_01",
                    nameof(IniciarAsync),
                    "No fue posible enviar el código de verificación. Intentá nuevamente.",
                    500,
                    default!);
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "2FA iniciado para persona {CodigoPersona}, sesión {SessionId}",
                    pendingAuth.Persona.CodigoPersona,
                    sessionId);
            }

            return OperationResult<DtoLogin2FARequired>.Ok(
                new DtoLogin2FARequired
                {
                    SessionId = sessionId,
                    MaskedEmail = EmailMaskingHelper.Mask(email)
                },
                nameof(IniciarAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al iniciar 2FA");

            return OperationResult<DtoLogin2FARequired>.IsFailed(
                "AUTH_2FA_INIT_99",
                nameof(IniciarAsync),
                "Error al iniciar la verificación de identidad.",
                500,
                default!);
        }
    }

    public async Task<OperationResult<DtoAuthenticationResponse>> VerificarCodigoAsync(
        string sessionId,
        string codigo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(codigo))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "AUTH_2FA_01",
                    nameof(VerificarCodigoAsync),
                    "La sesión y el código son requeridos.",
                    400,
                    default!);
            }

            var session = await _sessionStore.GetAsync(sessionId);

            if (session == null)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "AUTH_2FA_02",
                    nameof(VerificarCodigoAsync),
                    "La sesión de verificación no existe o ha expirado.",
                    401,
                    default!);
            }

            var maxAttempts = _configuration.GetValue<int?>("Authentication:TwoFactor:MaxCodeAttempts") ?? 5;

            if (CodigoExpirado(session))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "AUTH_2FA_06",
                    nameof(VerificarCodigoAsync),
                    "El código de verificación expiró. Solicitá uno nuevo para continuar.",
                    401,
                    default!);
            }

            var codigoHash = HashCodigo(codigo.Trim());
            if (!string.Equals(codigoHash, session.CodigoHash, StringComparison.Ordinal))
            {
                session.Intentos++;

                if (session.Intentos >= maxAttempts)
                {
                    await _sessionStore.DeleteAsync(sessionId);

                    _logger.LogWarning(
                        "Máximo de intentos 2FA superado para la sesión {SessionId}, persona {CodigoPersona}",
                        sessionId,
                        session.CodigoPersona);

                    return OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "AUTH_2FA_04",
                        nameof(VerificarCodigoAsync),
                        "Se superó el máximo de intentos de verificación. Por tu seguridad, iniciá el proceso nuevamente.",
                        401,
                        default!);
                }

                var ttl = await _sessionStore.GetTtlAsync(sessionId) ?? TimeSpan.FromMinutes(ObtenerSessionMinutes());
                await _sessionStore.UpdateAsync(sessionId, session, ttl);

                var remaining = maxAttempts - session.Intentos;

                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "AUTH_2FA_05",
                    nameof(VerificarCodigoAsync),
                    $"Código incorrecto. Te quedan {remaining} intento(s).",
                    401,
                    default!);
            }

            // Código correcto: eliminar sesión y retornar autenticación completa
            await _sessionStore.DeleteAsync(sessionId);
            await LimpiarRateLimitInicioAsync(session);

            var authResponse = new DtoAuthenticationResponse
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = session.CodigoPersona,
                    PrimerNombre = session.PrimerNombre,
                    SegundoNombre = session.SegundoNombre,
                    PrimerApellido = session.PrimerApellido,
                    SegundoApellido = session.SegundoApellido,
                    TipoPersona = session.TipoPersona,
                    Documento = session.Documento
                },
                AccessToken = session.AccessToken,
                RefreshToken = session.RefreshToken,
                RefreshTokenHash = session.RefreshTokenHash,
                Message = "Verificación completada. Los tokens han sido establecidos como cookies seguras."
            };

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "2FA verificado exitosamente para la persona {CodigoPersona}",
                    session.CodigoPersona);
            }

            return OperationResult<DtoAuthenticationResponse>.Ok(authResponse, nameof(VerificarCodigoAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al verificar el código 2FA para la sesión {SessionId}", sessionId);

            return OperationResult<DtoAuthenticationResponse>.IsFailed(
                "AUTH_2FA_99",
                nameof(VerificarCodigoAsync),
                "Error al verificar el código.",
                500,
                default!);
        }
    }

    public async Task<OperationResult<DtoLogin2FARequired>> ReenviarCodigoAsync(string sessionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return OperationResult<DtoLogin2FARequired>.IsFailed(
                    "AUTH_2FA_RESEND_01",
                    nameof(ReenviarCodigoAsync),
                    "La sesión es requerida.",
                    400,
                    default!);
            }

            var session = await _sessionStore.GetAsync(sessionId);

            if (session == null)
            {
                return OperationResult<DtoLogin2FARequired>.IsFailed(
                    "AUTH_2FA_02",
                    nameof(ReenviarCodigoAsync),
                    "La sesión de verificación no existe o ha expirado.",
                    401,
                    default!);
            }

            if (string.IsNullOrWhiteSpace(session.Email))
            {
                return OperationResult<DtoLogin2FARequired>.IsFailed(
                    "AUTH_2FA_RESEND_03",
                    nameof(ReenviarCodigoAsync),
                    "La sesión no tiene un email válido para reenviar el código.",
                    422,
                    default!);
            }

            var sessionMinutes = ObtenerSessionMinutes();
            var initLimit = _configuration.GetValue<int?>("Authentication:TwoFactor:MaxInitAttempts") ?? 3;
            var normalizedDoc = DocumentUtils.NormalizarDocumentoParaClave(session.Documento);
            var allowed = await _rateLimiter.IsAllowedAsync(
                $"2fa-init:{normalizedDoc}",
                initLimit,
                TimeSpan.FromMinutes(sessionMinutes));

            if (!allowed)
            {
                _logger.LogWarning(
                    "Límite de reenvíos 2FA superado para el documento {Doc}",
                    normalizedDoc);

                return OperationResult<DtoLogin2FARequired>.IsFailed(
                    "AUTH_2FA_RESEND_04",
                    nameof(ReenviarCodigoAsync),
                    $"Se superó el límite de solicitudes de verificación para esta cuenta. Intentá nuevamente en {sessionMinutes} minutos.",
                    429,
                    default!);
            }

            var ttl = await _sessionStore.GetTtlAsync(sessionId) ?? TimeSpan.FromMinutes(sessionMinutes);
            var codeLength = _configuration.GetValue<int?>("Authentication:TwoFactor:CodeLength") ?? 6;
            var codeMinutes = ObtenerCodeMinutes();
            var codigo = GenerarCodigo(codeLength);

            try
            {
                await EnviarCodigoMailAsync(session.Email, codigo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reenviar email 2FA a {Email}", session.Email);

                return OperationResult<DtoLogin2FARequired>.IsFailed(
                    "AUTH_2FA_RESEND_MAIL_01",
                    nameof(ReenviarCodigoAsync),
                    "No fue posible reenviar el código de verificación. Intentá nuevamente.",
                    500,
                    default!);
            }

            session.CodigoHash = HashCodigo(codigo);
            session.CodigoExpiresAtUtc = DateTime.UtcNow.AddMinutes(codeMinutes);
            session.Intentos = 0;

            await _sessionStore.UpdateAsync(sessionId, session, ttl);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "2FA reenviado para persona {CodigoPersona}, sesión {SessionId}",
                    session.CodigoPersona,
                    sessionId);
            }

            return OperationResult<DtoLogin2FARequired>.Ok(
                new DtoLogin2FARequired
                {
                    SessionId = sessionId,
                    MaskedEmail = EmailMaskingHelper.Mask(session.Email),
                    Message = "Se reenvió un nuevo código de verificación a tu correo electrónico."
                },
                nameof(ReenviarCodigoAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al reenviar el código 2FA para la sesión {SessionId}", sessionId);

            return OperationResult<DtoLogin2FARequired>.IsFailed(
                "AUTH_2FA_RESEND_99",
                nameof(ReenviarCodigoAsync),
                "Error al reenviar el código.",
                500,
                default!);
        }
    }

    private Task EnviarCodigoMailAsync(string email, string codigo)
    {
        var body = ConstruirMailBodyInstitucional(codigo);
        return _emailSender.SendAsync(email, "Tu código de verificación - Admisiones ORT", body);
    }

    private static string ConstruirMailBodyInstitucional(string codigo) =>
        $"""
        {EnvioMail.CabezalHTML()}
        {EnvioMail.Cabezal()}
        {EnvioMail.CuerpoConTitulo("Verificaci&oacute;n de identidad", DateTime.Now, "Estimado/a:")}
        {EnvioMail.CuerpoParrafos($"Recibimos una solicitud para ingresar al sitio de Admisiones. Para completar el inicio de sesi&oacute;n, ingres&aacute; el siguiente c&oacute;digo de verificaci&oacute;n:<br><br><strong style=\"font-size: 24px; letter-spacing: 6px;\">{codigo}</strong>")}
        {EnvioMail.CuerpoParrafos("Por seguridad, este c&oacute;digo vence en breve y puede usarse una sola vez. Si no realizaste esta solicitud, pod&eacute;s ignorar este mensaje.")}
        {EnvioMail.CuerpoParrafos("ORT nunca te solicitar&aacute; actualizar tu usuario, contrase&ntilde;a o datos de medios de pago electr&oacute;nicos por e-mail, tel&eacute;fono, SMS, WhatsApp ni redes sociales. M&aacute;s informaci&oacute;n en: <a href=\"https://www.ort.edu.uy/ciberseguridad\" target=\"_blank\" rel=\"noopener noreferrer\">www.ort.edu.uy/ciberseguridad</a>.")}
        {EnvioMail.CuerpoParrafos("Atentamente,<br>Departamento de Admisiones")}
        {EnvioMail.FinHtml}
        """;

    private static string GenerarCodigo(int length)
    {
        var digits = new char[length];
        for (var i = 0; i < length; i++)
            digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        return new string(digits);
    }

    private static string HashCodigo(string codigo)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codigo));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private int ObtenerSessionMinutes() =>
        _configuration.GetValue<int?>("Authentication:TwoFactor:SessionMinutes") ?? DefaultSessionMinutes;

    private int ObtenerCodeMinutes() =>
        _configuration.GetValue<int?>("Authentication:TwoFactor:CodeMinutes") ?? DefaultCodeMinutes;

    private static bool CodigoExpirado(DtoTwoFactorSession session) =>
        session.CodigoExpiresAtUtc <= DateTime.UtcNow;

    private async Task LimpiarRateLimitInicioAsync(DtoTwoFactorSession session)
    {
        try
        {
            await _rateLimiter.ClearAsync($"2fa-init:{DocumentUtils.NormalizarDocumentoParaClave(session.Documento)}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo limpiar el rate limit de inicio 2FA para la persona {CodigoPersona}.",
                session.CodigoPersona);
        }
    }
}
