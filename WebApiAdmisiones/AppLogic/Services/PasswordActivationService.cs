using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AppLogic.DTOs;
using AppLogic.IServices;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using MailORT;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Utilities;

namespace AppLogic.Services;

public class PasswordActivationService : IPasswordActivationService
{
    private const string ActivationPurpose = "password-activation";
    private const string RecoveryPurpose = "password-recovery";
    private const string SessionPurpose = "password-activation-session";
    private const string Issuer = "WebApiAdmisiones.PasswordActivation";
    private const string Audience = "AdmisionesWeb.PasswordActivation";
    private const string SistemaMail = "ADMISIONES";

    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IConfiguration _configuration;
    private readonly EnvioMail _envioMail;

    public PasswordActivationService(
        IUnitOfWorkFactory uowFactory,
        IConfiguration configuration,
        EnvioMail envioMail)
    {
        _uowFactory = uowFactory;
        _configuration = configuration;
        _envioMail = envioMail;
    }

    public async Task<OperationResult<object?>> EnviarMailLinkPasswordAsync(Persona persona, string originMethod)
    {
        try
        {
            if (persona == null)
            {
                return OperationResult<object?>.IsFailed(
                    "ACT_PAS_01",
                    originMethod,
                    "La persona es obligatoria para generar el link de contraseña.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(persona.Email))
            {
                return OperationResult<object?>.IsFailed(
                    "ACT_PAS_02",
                    originMethod,
                    "La persona no tiene un email configurado.",
                    400);
            }

            var token = GenerarToken(persona.CodigoPersona, ActivationPurpose, ObtenerHorasExpiracion());
            var hash = HashToken(token);

            var uow = _uowFactory.Create();
            var personaDb = uow.Personas.GetByKey(persona.CodigoPersona);
            if (personaDb == null)
            {
                return OperationResult<object?>.IsFailed(
                        "ACT_PAS_03",
                        originMethod,
                        "No se encontró la persona para guardar el token de activación.",
                    404);
            }

            personaDb.HashTokenPassword = hash;
            uow.Save();

            var link = ConstruirLink(token);
            var body = ConstruirBodyMailActivacion(persona, link);
            var from = _configuration["Mail:From"] ?? "admisiones@ort.edu.uy";

            await _envioMail.EnviarMail(
                from,
                new List<string> { persona.Email.Trim() },
                "Crea tu contraseña de Admisiones",
                body,
                sistema: SistemaMail);

            return OperationResult<object?>.IsSuccess(
                null,
                originMethod,
                "Registro realizado correctamente.");
        }
        catch (Exception ex)
        {
            return OperationResult<object?>.IsFailed(
                "ACT_PAS_99",
                originMethod,
                $"Error al enviar link de contraseña: {ex.Message}",
                500);
        }
    }

    public async Task<OperationResult<object?>> EnviarMailRecuperacionPasswordAsync(Persona persona, string originMethod)
    {
        try
        {
            if (persona == null)
            {
                return OperationResult<object?>.IsFailed(
                    "REC_LINK_01",
                    originMethod,
                    "La persona es obligatoria para generar el link de recuperación.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(persona.Email))
            {
                return OperationResult<object?>.IsFailed(
                    "REC_LINK_02",
                    originMethod,
                    "La persona no tiene un email configurado.",
                    400);
            }

            var token = GenerarToken(persona.CodigoPersona, RecoveryPurpose, ObtenerHorasExpiracion());
            var hash = HashToken(token);

            var uow = _uowFactory.Create();
            var personaDb = uow.Personas.GetByKey(persona.CodigoPersona);
            if (personaDb == null)
            {
                return OperationResult<object?>.IsFailed(
                    "REC_LINK_03",
                    originMethod,
                    "No se encontró la persona para guardar el token de recuperación.",
                    404);
            }

            personaDb.HashTokenPassword = hash;
            uow.Save();

            var link = ConstruirLink(token, "recovery");
            var body = ConstruirBodyMailRecuperacion(persona, link);
            var from = _configuration["Mail:From"] ?? "admisiones@ort.edu.uy";

            await _envioMail.EnviarMail(
                from,
                new List<string> { persona.Email.Trim() },
                "Recuperá tu contraseña de Admisiones",
                body,
                sistema: SistemaMail);

            return OperationResult<object?>.IsSuccess(
                null,
                originMethod,
                "Si los datos ingresados son correctos, recibirás un mail con instrucciones para recuperar tu contraseña.");
        }
        catch (Exception ex)
        {
            return OperationResult<object?>.IsFailed(
                "REC_LINK_99",
                originMethod,
                $"Error al enviar link de recuperación de contraseña: {ex.Message}",
                500);
        }
    }

    public Task<OperationResult<DtoPasswordActivationSession>> ActivarLinkPasswordAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Task.FromResult(OperationResult<DtoPasswordActivationSession>.IsFailed(
                    "ACT_LINK_01",
                    nameof(ActivarLinkPasswordAsync),
                    "El token es obligatorio.",
                    400,
                    default!));
            }

            var principal = ValidarJwt(token);
            var purpose = principal.FindFirst("purpose")?.Value;
            if (!EsPurposeLinkPasswordValido(purpose))
            {
                return Task.FromResult(OperationResult<DtoPasswordActivationSession>.IsFailed(
                    "ACT_LINK_02",
                    nameof(ActivarLinkPasswordAsync),
                    "El token no corresponde al flujo de activación de contraseña.",
                    401,
                    default!));
            }

            var codigoPersona = ObtenerCodigoPersona(principal);
            if (!codigoPersona.HasValue)
            {
                return Task.FromResult(OperationResult<DtoPasswordActivationSession>.IsFailed(
                    "ACT_LINK_03",
                    nameof(ActivarLinkPasswordAsync),
                    "El token no contiene una persona válida.",
                    401,
                    default!));
            }

            var uow = _uowFactory.Create();
            var persona = uow.Personas.GetByKey(codigoPersona.Value);
            var tokenHash = HashToken(token);
            if (persona == null ||
                string.IsNullOrWhiteSpace(persona.HashTokenPassword) ||
                !string.Equals(persona.HashTokenPassword, tokenHash, StringComparison.Ordinal))
            {
                return Task.FromResult(OperationResult<DtoPasswordActivationSession>.IsFailed(
                    "ACT_LINK_04",
                    nameof(ActivarLinkPasswordAsync),
                    "El link de activación es inválido o ya fue utilizado.",
                    401,
                    default!));
            }

            var sessionToken = GenerarToken(codigoPersona.Value, SessionPurpose, TimeSpan.FromMinutes(ObtenerMinutosSesion()));
            var response = new DtoPasswordActivationSession
            {
                CodigoPersona = codigoPersona.Value,
                SessionToken = sessionToken
            };

            return Task.FromResult(OperationResult<DtoPasswordActivationSession>.Ok(
                response,
                nameof(ActivarLinkPasswordAsync)));
        }
        catch (SecurityTokenExpiredException)
        {
            return Task.FromResult(OperationResult<DtoPasswordActivationSession>.IsFailed(
                "ACT_LINK_05",
                nameof(ActivarLinkPasswordAsync),
                "El link de activación expiró.",
                401,
                default!));
        }
        catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
        {
            return Task.FromResult(OperationResult<DtoPasswordActivationSession>.IsFailed(
                "ACT_LINK_06",
                nameof(ActivarLinkPasswordAsync),
                "El link de activación es inválido.",
                401,
                default!));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<DtoPasswordActivationSession>.IsFailed(
                "ACT_LINK_99",
                nameof(ActivarLinkPasswordAsync),
                $"Error al activar link de contraseña: {ex.Message}",
                500,
                default!));
        }
    }

    public OperationResult<long> ValidarSessionToken(string sessionToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return OperationResult<long>.IsFailed(
                    "ACT_SES_01",
                    nameof(ValidarSessionToken),
                    "Sesión temporal no encontrada.",
                    401,
                    default);
            }

            var principal = ValidarJwt(sessionToken);
            var purpose = principal.FindFirst("purpose")?.Value;
            if (!string.Equals(purpose, SessionPurpose, StringComparison.Ordinal))
            {
                return OperationResult<long>.IsFailed(
                    "ACT_SES_02",
                    nameof(ValidarSessionToken),
                    "Sesión temporal inválida.",
                    401,
                    default);
            }

            var codigoPersona = ObtenerCodigoPersona(principal);
            if (!codigoPersona.HasValue)
            {
                return OperationResult<long>.IsFailed(
                    "ACT_SES_03",
                    nameof(ValidarSessionToken),
                    "Sesión temporal sin persona válida.",
                    401,
                    default);
            }

            return OperationResult<long>.Ok(codigoPersona.Value, nameof(ValidarSessionToken));
        }
        catch (SecurityTokenExpiredException)
        {
            return OperationResult<long>.IsFailed(
                "ACT_SES_04",
                nameof(ValidarSessionToken),
                "La sesión temporal expiró.",
                401,
                default);
        }
        catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
        {
            return OperationResult<long>.IsFailed(
                "ACT_SES_05",
                nameof(ValidarSessionToken),
                "Sesión temporal inválida.",
                401,
                default);
        }
        catch (Exception ex)
        {
            return OperationResult<long>.IsFailed(
                "ACT_SES_99",
                nameof(ValidarSessionToken),
                $"Error al validar sesión temporal: {ex.Message}",
                500,
                default);
        }
    }

    private static string GenerarToken(long codigoPersona, string purpose, TimeSpan duration)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ObtenerSecretKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, codigoPersona.ToString(CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("purpose", purpose)
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(duration),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal ValidarJwt(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ObtenerSecretKey())),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        return new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
    }

    private static long? ObtenerCodigoPersona(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var codigoPersona)
            ? codigoPersona
            : null;
    }

    private static bool EsPurposeLinkPasswordValido(string? purpose)
    {
        return string.Equals(purpose, ActivationPurpose, StringComparison.Ordinal)
            || string.Equals(purpose, RecoveryPurpose, StringComparison.Ordinal);
    }

    private string ConstruirLink(string token, string? flow = null)
    {
        var baseUrl = _configuration["AdmisionesFrontend:CrearPasswordUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Falta configurar AdmisionesFrontend:CrearPasswordUrl.");
        }

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var link = $"{baseUrl}{separator}token={Uri.EscapeDataString(token)}";
        if (!string.IsNullOrWhiteSpace(flow))
        {
            link = $"{link}&flow={Uri.EscapeDataString(flow)}";
        }

        return link;
    }

    private static string ConstruirBodyMailActivacion(Persona persona, string link)
    {
        var nombre = HtmlEncoder.Default.Encode(persona.PrimerNombre?.Trim() ?? "usuario");
        var safeLink = HtmlEncoder.Default.Encode(link);

        return $"""
            <html>
            <body>
                <p>Hola {nombre},</p>
                <p>Tu registro en Admisiones quedó realizado correctamente.</p>
                <p>Para crear tu contraseña e ingresar al sitio, abrí el siguiente link:</p>
                <p><a href="{safeLink}">Crear contraseña</a></p>
                <p>Por seguridad, el link vence en el plazo indicado por el sistema y puede usarse una sola vez.</p>
            </body>
            </html>
            """;
    }

    private static string ConstruirBodyMailRecuperacion(Persona persona, string link)
    {
        var nombre = HtmlEncoder.Default.Encode(persona.PrimerNombre?.Trim() ?? "usuario");
        var safeLink = HtmlEncoder.Default.Encode(link);

        return $"""
            <html>
            <body>
                <p>Hola {nombre},</p>
                <p>Recibimos una solicitud para recuperar tu contraseña de Admisiones.</p>
                <p>Para crear una nueva contraseñaa e ingresar al sitio, abrá­ el siguiente link:</p>
                <p><a href="{safeLink}">Recuperar contraseña</a></p>
                <p>Por seguridad, el link vence en el plazo indicado por el sistema y puede usarse una sola vez.</p>
                <p>Si no solicitaste este cambio, podés ignorar este mensaje.</p>
            </body>
            </html>
            """;
    }

    private static string ObtenerSecretKey()
    {
        var secret = Environment.GetEnvironmentVariable("PASSWORD_ACTIVATION_SECRET_KEY");

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Falta configurar PASSWORD_ACTIVATION_SECRET_KEY.");
        }

        if (Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("PASSWORD_ACTIVATION_SECRET_KEY debe tener al menos 32 bytes.");
        }

        return secret;
    }

    private TimeSpan ObtenerHorasExpiracion()
    {
        var hours = _configuration.GetValue<double?>("PasswordActivation:ExpireHours") ?? 24;
        return TimeSpan.FromHours(hours);
    }

    private double ObtenerMinutosSesion()
    {
        return _configuration.GetValue<double?>("PasswordActivation:SessionMinutes") ?? 15;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
