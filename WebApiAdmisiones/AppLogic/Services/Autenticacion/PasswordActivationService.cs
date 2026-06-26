using AppLogic.Dtos.Autenticacion;
using AppLogic.Dtos.Registro;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AppLogic.Helpers;
using AppLogic.IServices.Autenticacion;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using MailORT;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Utilities;

namespace AppLogic.Services.Autenticacion;

public class PasswordActivationService : IPasswordActivationService
{
    private const string ActivationPurpose = "password-activation";
    private const string RecoveryPurpose = "password-recovery";
    private const string SessionPurpose = "password-activation-session";
    private const string NuevaPersonaPurpose = "nueva-persona-activacion";
    private const string NuevaPersonaSessionPurpose = "nueva-persona-session";
    private const string Issuer = "WebApiAdmisiones";
    private const string Audience = "AdmisionesPassword";
    private const string SistemaMail = "ADMISIONES";

    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IConfiguration _configuration;
    private readonly EnvioMail _envioMail;
    private readonly IHashTokenStore _hashTokenStore;
    private readonly IDatabase _redisDb;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public PasswordActivationService(
        IUnitOfWorkFactory uowFactory,
        IConfiguration configuration,
        EnvioMail envioMail,
        IHashTokenStore hashTokenStore,
        IConnectionMultiplexer redis)
    {
        _uowFactory = uowFactory;
        _configuration = configuration;
        _envioMail = envioMail;
        _hashTokenStore = hashTokenStore;
        _redisDb = redis.GetDatabase();
    }

    private sealed record PasswordMailFlow(
        string Purpose,
        string? QueryFlow,
        string Subject,
        Func<Persona, string, string> ConstruirBody,
        string MensajeOk,
        string CodigoPersonaObligatoria,
        string CodigoEmailObligatorio,
        string CodigoPersonaNoEncontrada,
        string CodigoErrorGeneral,
        string Descripcion);

    public async Task<OperationResult<object?>> EnviarMailLinkPasswordAsync(Persona persona, string originMethod)
    {
        return await EnviarMailPasswordAsync(
            persona,
            originMethod,
            new PasswordMailFlow(
                ActivationPurpose,
                QueryFlow: null,
                "Crea tu contraseña de Admisiones",
                PasswordMailTemplateHelper.ConstruirMailActivacion,
                "Registro realizado correctamente.",
                "ACT_PAS_01",
                "ACT_PAS_02",
                "ACT_PAS_03",
                "ACT_PAS_99",
                "activación de contraseña"));
    }

    public async Task<OperationResult<object?>> EnviarMailRecuperacionPasswordAsync(Persona persona, string originMethod)
    {
        return await EnviarMailPasswordAsync(
            persona,
            originMethod,
            new PasswordMailFlow(
                RecoveryPurpose,
                "recovery",
                "Recuperá tu contraseña de Admisiones",
                PasswordMailTemplateHelper.ConstruirMailRecuperacion,
                "Si los datos ingresados son correctos, recibirás un mail con instrucciones para recuperar tu contraseña.",
                "REC_LINK_01",
                "REC_LINK_02",
                "REC_LINK_03",
                "REC_LINK_99",
                "recuperación de contraseña"));
    }

    /// <inheritdoc />
    public async Task<OperationResult<object?>> EnviarMailNuevaPersonaAsync(string flowId, string email, string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return OperationResult<object?>.IsFailed(
                    "ACT_NUP_01",
                    nameof(EnviarMailNuevaPersonaAsync),
                    "El email es obligatorio para enviar el link de activación.",
                    400);
            }

            var link = PasswordActivationLinkBuilder.ConstruirLink(_configuration, token, flow: "registration");

            // Reutilizar plantilla de activación pero sin persona (solo necesitamos el link)
            var body = PasswordMailTemplateHelper.ConstruirMailActivacionSinPersona(link);
            var from = _configuration["Mail:From"] ?? "admisiones@ort.edu.uy";

            await _envioMail.EnviarMail(
                from,
                new List<string> { email.Trim() },
                "Crea tu contraseña de Admisiones",
                body,
                sistema: SistemaMail);

            return OperationResult<object?>.IsSuccess(
                null,
                nameof(EnviarMailNuevaPersonaAsync),
                "Registro realizado correctamente.");
        }
        catch (Exception ex)
        {
            return OperationResult<object?>.IsFailed(
                "ACT_NUP_99",
                nameof(EnviarMailNuevaPersonaAsync),
                $"Error al enviar link de activación para nueva persona: {ex.Message}",
                500);
        }
    }

    private async Task<OperationResult<object?>> EnviarMailPasswordAsync(
        Persona persona,
        string originMethod,
        PasswordMailFlow flow)
    {
        try
        {
            if (persona == null)
            {
                return OperationResult<object?>.IsFailed(
                    flow.CodigoPersonaObligatoria,
                    originMethod,
                    $"La persona es obligatoria para generar el link de {flow.Descripcion}.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(persona.Email))
            {
                return OperationResult<object?>.IsFailed(
                    flow.CodigoEmailObligatorio,
                    originMethod,
                    "La persona no tiene un email configurado.",
                    400);
            }

            var token = GenerarTokenPersona(persona.CodigoPersona, flow.Purpose, ObtenerHorasExpiracion());
            var hash = HashToken(token);

            using var uow = _uowFactory.Create();
            var personaDb = uow.Personas.GetByKey(persona.CodigoPersona);
            if (personaDb == null)
            {
                return OperationResult<object?>.IsFailed(
                    flow.CodigoPersonaNoEncontrada,
                    originMethod,
                    $"No se encontró la persona para guardar el token de {flow.Descripcion}.",
                    404);
            }

            // Almacenar hash en Redis
            await _hashTokenStore.StoreAsync(
                persona.CodigoPersona.ToString(CultureInfo.InvariantCulture),
                hash,
                ObtenerHorasExpiracion());

            var link = PasswordActivationLinkBuilder.ConstruirLink(_configuration, token, flow.QueryFlow);
            var body = flow.ConstruirBody(persona, link);
            var from = _configuration["Mail:From"] ?? "admisiones@ort.edu.uy";

            await _envioMail.EnviarMail(
                from,
                new List<string> { persona.Email.Trim() },
                flow.Subject,
                body,
                sistema: SistemaMail);

            return OperationResult<object?>.IsSuccess(
                null,
                originMethod,
                flow.MensajeOk);
        }
        catch (Exception ex)
        {
            return OperationResult<object?>.IsFailed(
                flow.CodigoErrorGeneral,
                originMethod,
                $"Error al enviar link de {flow.Descripcion}: {ex.Message}",
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

            // Flujo nueva persona (sub = flowId)
            if (string.Equals(purpose, NuevaPersonaPurpose, StringComparison.Ordinal))
            {
                return ActivarTokenNuevaPersonaAsync(token, principal);
            }

            // Flujo persona existente (sub = codigoPersona)
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

            return ActivarTokenPersonaExistenteAsync(token, codigoPersona.Value);
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

    private async Task<OperationResult<DtoPasswordActivationSession>> ActivarTokenPersonaExistenteAsync(
        string token,
        long codigoPersona)
    {
        var tokenHash = HashToken(token);
        var storedHash = await _hashTokenStore.GetAsync(codigoPersona.ToString(CultureInfo.InvariantCulture));

        if (string.IsNullOrWhiteSpace(storedHash) ||
            !string.Equals(storedHash, tokenHash, StringComparison.Ordinal))
        {
            return OperationResult<DtoPasswordActivationSession>.IsFailed(
                "ACT_LINK_04",
                nameof(ActivarLinkPasswordAsync),
                "El link de activación es inválido o ya fue utilizado.",
                401,
                default!);
        }

        var sessionToken = GenerarTokenPersona(codigoPersona, SessionPurpose, TimeSpan.FromMinutes(ObtenerMinutosSesion()));
        var response = new DtoPasswordActivationSession
        {
            CodigoPersona = codigoPersona,
            SessionToken = sessionToken
        };

        return OperationResult<DtoPasswordActivationSession>.Ok(response, nameof(ActivarLinkPasswordAsync));
    }

    private async Task<OperationResult<DtoPasswordActivationSession>> ActivarTokenNuevaPersonaAsync(
        string token,
        ClaimsPrincipal principal)
    {
        var flowId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(flowId))
        {
            return OperationResult<DtoPasswordActivationSession>.IsFailed(
                "ACT_LINK_NUP_01",
                nameof(ActivarLinkPasswordAsync),
                "El token no contiene un FlowId válido.",
                401,
                default!);
        }

        var pendingKey = $"registro:pending:{flowId}";
        var pendingJson = await _redisDb.StringGetAsync(pendingKey);

        if (!pendingJson.HasValue)
        {
            return OperationResult<DtoPasswordActivationSession>.IsFailed(
                "ACT_LINK_NUP_02",
                nameof(ActivarLinkPasswordAsync),
                "El link de activación es inválido, ya fue utilizado o expiró.",
                401,
                default!);
        }

        DtoRegistroPendingPersona? pending;
        try
        {
            pending = JsonSerializer.Deserialize<DtoRegistroPendingPersona>(pendingJson.ToString(), JsonOptions);
        }
        catch
        {
            pending = null;
        }

        if (pending == null)
        {
            return OperationResult<DtoPasswordActivationSession>.IsFailed(
                "ACT_LINK_NUP_03",
                nameof(ActivarLinkPasswordAsync),
                "No se pudo recuperar los datos del registro pendiente.",
                500,
                default!);
        }

        var tokenHash = HashToken(token);
        if (!string.Equals(pending.TokenHash, tokenHash, StringComparison.Ordinal))
        {
            return OperationResult<DtoPasswordActivationSession>.IsFailed(
                "ACT_LINK_NUP_04",
                nameof(ActivarLinkPasswordAsync),
                "El link de activación es inválido o ya fue utilizado.",
                401,
                default!);
        }

        var sessionToken = GenerarTokenFlowId(flowId, NuevaPersonaSessionPurpose, TimeSpan.FromMinutes(ObtenerMinutosSesion()));
        var response = new DtoPasswordActivationSession
        {
            Documento = pending.Documento,
            SessionToken = sessionToken
        };

        return OperationResult<DtoPasswordActivationSession>.Ok(response, nameof(ActivarLinkPasswordAsync));
    }

    public OperationResult<DtoValidatedSession> ValidarSessionToken(string sessionToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return OperationResult<DtoValidatedSession>.IsFailed(
                    "ACT_SES_01",
                    nameof(ValidarSessionToken),
                    "Sesión temporal no encontrada.",
                    401,
                    default!);
            }

            var principal = ValidarJwt(sessionToken);
            var purpose = principal.FindFirst("purpose")?.Value;

            if (string.Equals(purpose, NuevaPersonaSessionPurpose, StringComparison.Ordinal))
            {
                var flowId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(flowId))
                {
                    return OperationResult<DtoValidatedSession>.IsFailed(
                        "ACT_SES_NUP_01",
                        nameof(ValidarSessionToken),
                        "Sesión temporal sin FlowId válido.",
                        401,
                        default!);
                }

                return OperationResult<DtoValidatedSession>.Ok(
                    new DtoValidatedSession { FlowId = flowId, Purpose = NuevaPersonaSessionPurpose },
                    nameof(ValidarSessionToken));
            }

            if (!string.Equals(purpose, SessionPurpose, StringComparison.Ordinal))
            {
                return OperationResult<DtoValidatedSession>.IsFailed(
                    "ACT_SES_02",
                    nameof(ValidarSessionToken),
                    "Sesión temporal inválida.",
                    401,
                    default!);
            }

            var codigoPersona = ObtenerCodigoPersona(principal);
            if (!codigoPersona.HasValue)
            {
                return OperationResult<DtoValidatedSession>.IsFailed(
                    "ACT_SES_03",
                    nameof(ValidarSessionToken),
                    "Sesión temporal sin persona válida.",
                    401,
                    default!);
            }

            return OperationResult<DtoValidatedSession>.Ok(
                new DtoValidatedSession { CodigoPersona = codigoPersona.Value, Purpose = SessionPurpose },
                nameof(ValidarSessionToken));
        }
        catch (SecurityTokenExpiredException)
        {
            return OperationResult<DtoValidatedSession>.IsFailed(
                "ACT_SES_04",
                nameof(ValidarSessionToken),
                "La sesión temporal expiró.",
                401,
                default!);
        }
        catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
        {
            return OperationResult<DtoValidatedSession>.IsFailed(
                "ACT_SES_05",
                nameof(ValidarSessionToken),
                "Sesión temporal inválida.",
                401,
                default!);
        }
        catch (Exception ex)
        {
            return OperationResult<DtoValidatedSession>.IsFailed(
                "ACT_SES_99",
                nameof(ValidarSessionToken),
                $"Error al validar sesión temporal: {ex.Message}",
                500,
                default!);
        }
    }

    /// <summary>Genera un JWT donde el subject es un codigoPersona (long).</summary>
    private static string GenerarTokenPersona(long codigoPersona, string purpose, TimeSpan duration)
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

    /// <summary>Genera un JWT donde el subject es un flowId (string GUID).</summary>
    private static string GenerarTokenFlowId(string flowId, string purpose, TimeSpan duration)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ObtenerSecretKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, flowId),
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

    internal static string GenerarTokenFlowIdPublic(string flowId, string purpose, TimeSpan duration)
        => GenerarTokenFlowId(flowId, purpose, duration);

    private static ClaimsPrincipal ValidarJwt(string token)
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

    internal static string ObtenerSecretKeyPublic() => ObtenerSecretKey();

    private TimeSpan ObtenerHorasExpiracion()
    {
        var hours = _configuration.GetValue<double?>("PasswordActivation:ExpireHours") ?? 24;
        return TimeSpan.FromHours(hours);
    }

    private double ObtenerMinutosSesion()
    {
        return _configuration.GetValue<double?>("PasswordActivation:SessionMinutes") ?? 15;
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
