using AppLogic.DTOs;
using MailORT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Utilities;

namespace WebApiAdmisiones.Security
{
    /// <summary>
    /// Implementación del servicio de autenticación de dos factores.
    /// Almacena sesiones temporales en Redis y envía códigos de verificación por email.
    /// </summary>
    public class DosFactoresAuthService : IDosFactoresAuthService
    {
        private const string SistemaMail = "ADMISIONES";

        private readonly IConnectionMultiplexer _redis;
        private readonly IRedisRateLimiterService _rateLimiter;
        private readonly EnvioMail _envioMail;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DosFactoresAuthService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public DosFactoresAuthService(
            IConnectionMultiplexer redis,
            IRedisRateLimiterService rateLimiter,
            EnvioMail envioMail,
            IConfiguration configuration,
            ILogger<DosFactoresAuthService> logger)
        {
            _redis = redis;
            _rateLimiter = rateLimiter;
            _envioMail = envioMail;
            _configuration = configuration;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<OperationResult<DtoLogin2FARequired>> IniciarAsync(
            DtoAuthenticationResponse pendingAuth,
            string email)
        {
            try
            {
                var normalizedDoc = NormalizeDoc(pendingAuth.Persona.Documento);
                var initLimit = _configuration.GetValue<int?>("Authentication:TwoFactor:MaxInitAttempts") ?? 3;
                var sessionMinutes = _configuration.GetValue<int?>("Authentication:TwoFactor:SessionMinutes") ?? 10;
                var initWindow = TimeSpan.FromMinutes(sessionMinutes);

                // Verificar rate limit de inicios de 2FA por usuario (anti email-bombing)
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

                var session = new TwoFactorSession
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
                    Email = email,
                    Intentos = 0
                };

                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(session, _jsonOptions);
                await db.StringSetAsync(
                    $"2fa:session:{sessionId}",
                    json,
                    TimeSpan.FromMinutes(sessionMinutes));

                // Enviar código por email. Si falla, eliminar la sesión.
                try
                {
                    await EnviarCodigoMailAsync(email, codigo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar email 2FA a {Email}", email);
                    await db.KeyDeleteAsync($"2fa:session:{sessionId}");

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
                    new DtoLogin2FARequired { SessionId = sessionId },
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

                var db = _redis.GetDatabase();
                var sessionJson = await db.StringGetAsync($"2fa:session:{sessionId}");

                if (!sessionJson.HasValue)
                {
                    return OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "AUTH_2FA_02",
                        nameof(VerificarCodigoAsync),
                        "La sesión de verificación no existe o ha expirado.",
                        401,
                        default!);
                }

                TwoFactorSession? session;
                try
                {
                    session = JsonSerializer.Deserialize<TwoFactorSession>(sessionJson.ToString(), _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error al deserializar la sesión 2FA {SessionId}", sessionId);
                    await db.KeyDeleteAsync($"2fa:session:{sessionId}");

                    return OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "AUTH_2FA_03",
                        nameof(VerificarCodigoAsync),
                        "Error al procesar la sesión de verificación.",
                        500,
                        default!);
                }

                if (session == null)
                {
                    await db.KeyDeleteAsync($"2fa:session:{sessionId}");

                    return OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "AUTH_2FA_03",
                        nameof(VerificarCodigoAsync),
                        "Error al procesar la sesión de verificación.",
                        500,
                        default!);
                }

                var maxAttempts = _configuration.GetValue<int?>("Authentication:TwoFactor:MaxCodeAttempts") ?? 5;

                // Verificar código
                var codigoHash = HashCodigo(codigo.Trim());
                if (!string.Equals(codigoHash, session.CodigoHash, StringComparison.Ordinal))
                {
                    session.Intentos++;

                    if (session.Intentos >= maxAttempts)
                    {
                        await db.KeyDeleteAsync($"2fa:session:{sessionId}");

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

                    // Actualizar sesión con los intentos incrementados, preservando el TTL restante
                    var ttl = await db.KeyTimeToLiveAsync($"2fa:session:{sessionId}");
                    var updatedJson = JsonSerializer.Serialize(session, _jsonOptions);
                    await db.StringSetAsync($"2fa:session:{sessionId}", updatedJson, ttl);

                    var remaining = maxAttempts - session.Intentos;

                    return OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "AUTH_2FA_05",
                        nameof(VerificarCodigoAsync),
                        $"Código incorrecto. Te quedan {remaining} intento(s).",
                        401,
                        default!);
                }

                // Código correcto: eliminar sesión y retornar autenticación completa
                await db.KeyDeleteAsync($"2fa:session:{sessionId}");

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

                _logger.LogInformation(
                    "2FA verificado exitosamente para la persona {CodigoPersona}",
                    session.CodigoPersona);

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

        private async Task EnviarCodigoMailAsync(string email, string codigo)
        {
            var from = _configuration["Mail:From"] ?? "admisiones@ort.edu.uy";
            var body = ConstruirMailBody(codigo);

            await _envioMail.EnviarMail(
                from,
                new List<string> { email.Trim() },
                "Tu código de verificación - Admisiones ORT",
                body,
                sistema: SistemaMail);
        }

        private static string ConstruirMailBody(string codigo) =>
            $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto;">
              <h2 style="color: #1a73e8;">Verificación de identidad</h2>
              <p>Para completar tu inicio de sesión en Admisiones ORT, ingresá el siguiente código:</p>
              <div style="font-size: 36px; font-weight: bold; letter-spacing: 10px; color: #1a73e8;
                          background: #f0f4ff; padding: 16px 24px; border-radius: 8px;
                          display: inline-block; margin: 16px 0;">{codigo}</div>
              <p style="color: #666; font-size: 14px;">
                Este código expirará en breve. Si no realizaste esta solicitud, ignorá este mensaje.
              </p>
            </body>
            </html>
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

        private static string NormalizeDoc(string? doc)
        {
            if (string.IsNullOrWhiteSpace(doc))
                return "unknown";

            return doc.Replace(".", "").Replace("-", "").Replace(" ", "")
                      .Trim().ToLowerInvariant();
        }
    }

    /// <summary>
    /// Modelo interno para la sesión 2FA almacenada en Redis.
    /// Contiene los tokens pendientes y el hash del código de verificación.
    /// </summary>
    internal sealed class TwoFactorSession
    {
        public long CodigoPersona { get; set; }
        public string? PrimerNombre { get; set; }
        public string? SegundoNombre { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? TipoPersona { get; set; }
        public string? Documento { get; set; }

        // Tokens pendientes de establecer como cookies tras la verificación
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? RefreshTokenHash { get; set; }

        // Datos de verificación
        public string? CodigoHash { get; set; }
        public string? Email { get; set; }
        public int Intentos { get; set; }
    }
}
