using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.Authentication.Dtos;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AppLogic.Authentication.Rules;
using AppLogic.Authentication.Interfaces;
using AppLogic.Platform.Email;
using AppLogic.Authentication.Security;
using AppLogic.Platform.Serialization;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Utilities;

namespace AppLogic.Authentication.Services;

public class PasswordActivationService : IPasswordActivationService
{
    private const string ActivationPurpose = "password-activation";
    private const string RecoveryPurpose = "password-recovery";
    private const string SessionPurpose = "password-activation-session";
    private const string NuevaPersonaPurpose = "nueva-persona-activacion";
    private const string NuevaPersonaSessionPurpose = "nueva-persona-session";
    private const string Issuer = "WebApiAdmisiones";
    private const string Audience = "AdmisionesPassword";
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly IHashTokenStore _hashTokenStore;
    private readonly IPendingPersonStore _pendingPersonaStore;
    private readonly ILogger<PasswordActivationService> _logger;

    private const string ErrorInesperadoLog = "Error inesperado en {Metodo}";

    public PasswordActivationService(
        IUnitOfWorkFactory uowFactory,
        IConfiguration configuration,
        IEmailSender emailSender,
        IHashTokenStore hashTokenStore,
        IPendingPersonStore pendingPersonaStore,
        ILogger<PasswordActivationService> logger)
    {
        _uowFactory = uowFactory;
        _configuration = configuration;
        _emailSender = emailSender;
        _hashTokenStore = hashTokenStore;
        _pendingPersonaStore = pendingPersonaStore;
        _logger = logger;
    }

    private sealed record PasswordMailFlow(
        string Purpose,
        string? QueryFlow,
        string Subject,
        Func<Persona, string, string> BuildBody,
        string MensajeOk,
        string CodigoPersonaObligatoria,
        string CodigoEmailObligatorio,
        string CodigoPersonaNoEncontrada,
        string CodigoErrorGeneral,
        string Descripcion);

    public async Task<OperationResult<object?>> SendPasswordLinkMailAsync(Persona person, string originMethod)
    {
        return await SendPasswordMailAsync(
            person,
            originMethod,
            new PasswordMailFlow(
                ActivationPurpose,
                QueryFlow: null,
                "Crea tu contraseña de Admisiones",
                PasswordMailTemplate.BuildActivationMail,
                "Registro realizado correctamente.",
                "ACT_PAS_01",
                "ACT_PAS_02",
                "ACT_PAS_03",
                "ACT_PAS_99",
                "activación de contraseña"));
    }

    public async Task<OperationResult<object?>> SendPasswordRecoveryMailAsync(Persona person, string originMethod)
    {
        return await SendPasswordMailAsync(
            person,
            originMethod,
            new PasswordMailFlow(
                RecoveryPurpose,
                "recovery",
                "Recuperá tu contraseña de Admisiones",
                PasswordMailTemplate.BuildRecoveryMail,
                "Mail de recuperación enviado.",
                "REC_LINK_01",
                "REC_LINK_02",
                "REC_LINK_03",
                "REC_LINK_99",
                "recuperación de contraseña"));
    }

    /// <inheritdoc />
    public async Task<OperationResult<object?>> SendNewPersonMailAsync(string flowId, string email, string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return OperationResult<object?>.IsFailed(
                    "ACT_NUP_01",
                    nameof(SendNewPersonMailAsync),
                    "El email es obligatorio para enviar el link de activación.",
                    400);
            }

            var link = PasswordActivationLinkBuilder.BuildLink(_configuration, token, flow: "registration");

            // Reutilizar plantilla de activación pero sin persona (solo necesitamos el link)
            var body = PasswordMailTemplate.BuildActivationMailWithoutPerson(link);

            await _emailSender.SendAsync(
                email.Trim(),
                "Crea tu contraseña de Admisiones",
                body);

            return OperationResult<object?>.IsSuccess(
                null,
                nameof(SendNewPersonMailAsync),
                "Registro realizado correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorInesperadoLog, nameof(SendNewPersonMailAsync));
            return OperationResult<object?>.IsFailed(
                "ACT_NUP_99",
                nameof(SendNewPersonMailAsync),
                "Error al enviar link de activación para nueva persona.",
                500);
        }
    }

    private async Task<OperationResult<object?>> SendPasswordMailAsync(
        Persona person,
        string originMethod,
        PasswordMailFlow flow)
    {
        try
        {
            if (person == null)
            {
                return OperationResult<object?>.IsFailed(
                    flow.CodigoPersonaObligatoria,
                    originMethod,
                    $"La persona es obligatoria para generar el link de {flow.Descripcion}.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(person.Email))
            {
                return OperationResult<object?>.IsFailed(
                    flow.CodigoEmailObligatorio,
                    originMethod,
                    "La persona no tiene un email configurado.",
                    400);
            }

            var token = GeneratePersonToken(person.CodigoPersona, flow.Purpose, GetExpirationHours());
            var hash = HashToken(token);

            using var uow = _uowFactory.Create();
            var personFromDb = uow.Personas.GetByKey(person.CodigoPersona);
            if (personFromDb == null)
            {
                return OperationResult<object?>.IsFailed(
                    flow.CodigoPersonaNoEncontrada,
                    originMethod,
                    $"No se encontró la persona para guardar el token de {flow.Descripcion}.",
                    404);
            }

            // Almacenar hash en Redis
            await _hashTokenStore.StoreAsync(
                person.CodigoPersona.ToString(CultureInfo.InvariantCulture),
                hash,
                GetExpirationHours());

            var link = PasswordActivationLinkBuilder.BuildLink(_configuration, token, flow.QueryFlow);
            var body = flow.BuildBody(person, link);

            await _emailSender.SendAsync(
                person.Email.Trim(),
                flow.Subject,
                body);

            return OperationResult<object?>.IsSuccess(
                null,
                originMethod,
                flow.MensajeOk);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorInesperadoLog, originMethod);
            return OperationResult<object?>.IsFailed(
                flow.CodigoErrorGeneral,
                originMethod,
                $"Error al enviar link de {flow.Descripcion}.",
                500);
        }
    }

    public Task<OperationResult<PasswordActivationSession>> ActivatePasswordLinkAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Task.FromResult(OperationResult<PasswordActivationSession>.IsFailed(
                    "ACT_LINK_01",
                    nameof(ActivatePasswordLinkAsync),
                    "El token es obligatorio.",
                    400,
                    default!));
            }

            var principal = ValidateJwt(token);
            var purpose = principal.FindFirst("purpose")?.Value;

            // Flujo nueva persona (sub = flowId)
            if (string.Equals(purpose, NuevaPersonaPurpose, StringComparison.Ordinal))
            {
                return ActivateNewPersonTokenAsync(token, principal);
            }

            // Flujo persona existente (sub = personId)
            if (!IsValidPasswordLinkPurpose(purpose))
            {
                return Task.FromResult(OperationResult<PasswordActivationSession>.IsFailed(
                    "ACT_LINK_02",
                    nameof(ActivatePasswordLinkAsync),
                    "El token no corresponde al flujo de activación de contraseña.",
                    401,
                    default!));
            }

            var personId = GetPersonId(principal);
            if (!personId.HasValue)
            {
                return Task.FromResult(OperationResult<PasswordActivationSession>.IsFailed(
                    "ACT_LINK_03",
                    nameof(ActivatePasswordLinkAsync),
                    "El token no contiene una persona válida.",
                    401,
                    default!));
            }

            return ActivateExistingPersonTokenAsync(token, personId.Value);
        }
        catch (SecurityTokenExpiredException)
        {
            return Task.FromResult(OperationResult<PasswordActivationSession>.IsFailed(
                "ACT_LINK_05",
                nameof(ActivatePasswordLinkAsync),
                "El link de activación expiró.",
                401,
                default!));
        }
        catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
        {
            return Task.FromResult(OperationResult<PasswordActivationSession>.IsFailed(
                "ACT_LINK_06",
                nameof(ActivatePasswordLinkAsync),
                "El link de activación es inválido.",
                401,
                default!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorInesperadoLog, nameof(ActivatePasswordLinkAsync));
            return Task.FromResult(OperationResult<PasswordActivationSession>.IsFailed(
                "ACT_LINK_99",
                nameof(ActivatePasswordLinkAsync),
                "Error al activar link de contraseña.",
                500,
                default!));
        }
    }

    private async Task<OperationResult<PasswordActivationSession>> ActivateExistingPersonTokenAsync(
        string token,
        long personId)
    {
        var tokenHash = HashToken(token);
        var storedHash = await _hashTokenStore.GetAsync(personId.ToString(CultureInfo.InvariantCulture));

        if (string.IsNullOrWhiteSpace(storedHash) ||
            !string.Equals(storedHash, tokenHash, StringComparison.Ordinal))
        {
            return OperationResult<PasswordActivationSession>.IsFailed(
                "ACT_LINK_04",
                nameof(ActivatePasswordLinkAsync),
                "El link de activación es inválido o ya fue utilizado.",
                401,
                default!);
        }

        var sessionToken = GeneratePersonToken(personId, SessionPurpose, TimeSpan.FromMinutes(GetSessionMinutes()));
        var response = new PasswordActivationSession
        {
            PersonId = personId,
            SessionToken = sessionToken
        };

        return OperationResult<PasswordActivationSession>.Ok(response, nameof(ActivatePasswordLinkAsync));
    }

    private async Task<OperationResult<PasswordActivationSession>> ActivateNewPersonTokenAsync(
        string token,
        ClaimsPrincipal principal)
    {
        var flowId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(flowId))
        {
            return OperationResult<PasswordActivationSession>.IsFailed(
                "ACT_LINK_NUP_01",
                nameof(ActivatePasswordLinkAsync),
                "El token no contiene un FlowId válido.",
                401,
                default!);
        }

        var pendingJson = await _pendingPersonaStore.GetRawAsync(flowId);

        if (pendingJson == null)
        {
            return OperationResult<PasswordActivationSession>.IsFailed(
                "ACT_LINK_NUP_02",
                nameof(ActivatePasswordLinkAsync),
                "El link de activación es inválido, ya fue utilizado o expiró.",
                401,
                default!);
        }

        var pending = JsonSerialization.TryDeserialize<PendingPerson>(
            pendingJson,
            JsonSerializationDefaults.Redis);

        if (pending == null)
        {
            return OperationResult<PasswordActivationSession>.IsFailed(
                "ACT_LINK_NUP_03",
                nameof(ActivatePasswordLinkAsync),
                "No se pudo recuperar los datos del registro pendiente.",
                500,
                default!);
        }

        var tokenHash = HashToken(token);
        if (!string.Equals(pending.TokenHash, tokenHash, StringComparison.Ordinal))
        {
            return OperationResult<PasswordActivationSession>.IsFailed(
                "ACT_LINK_NUP_04",
                nameof(ActivatePasswordLinkAsync),
                "El link de activación es inválido o ya fue utilizado.",
                401,
                default!);
        }

        var sessionToken = GenerateFlowIdToken(flowId, NuevaPersonaSessionPurpose, TimeSpan.FromMinutes(GetSessionMinutes()));
        var response = new PasswordActivationSession
        {
            DocumentNumber = pending.DocumentNumber,
            SessionToken = sessionToken
        };

        return OperationResult<PasswordActivationSession>.Ok(response, nameof(ActivatePasswordLinkAsync));
    }

    public OperationResult<ValidatedSession> ValidateSessionToken(string sessionToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return OperationResult<ValidatedSession>.IsFailed(
                    "ACT_SES_01",
                    nameof(ValidateSessionToken),
                    "Sesión temporal no encontrada.",
                    401,
                    default!);
            }

            var principal = ValidateJwt(sessionToken);
            var purpose = principal.FindFirst("purpose")?.Value;

            if (string.Equals(purpose, NuevaPersonaSessionPurpose, StringComparison.Ordinal))
            {
                var flowId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(flowId))
                {
                    return OperationResult<ValidatedSession>.IsFailed(
                        "ACT_SES_NUP_01",
                        nameof(ValidateSessionToken),
                        "Sesión temporal sin FlowId válido.",
                        401,
                        default!);
                }

                return OperationResult<ValidatedSession>.Ok(
                    new ValidatedSession { FlowId = flowId, Purpose = NuevaPersonaSessionPurpose },
                    nameof(ValidateSessionToken));
            }

            if (!string.Equals(purpose, SessionPurpose, StringComparison.Ordinal))
            {
                return OperationResult<ValidatedSession>.IsFailed(
                    "ACT_SES_02",
                    nameof(ValidateSessionToken),
                    "Sesión temporal inválida.",
                    401,
                    default!);
            }

            var personId = GetPersonId(principal);
            if (!personId.HasValue)
            {
                return OperationResult<ValidatedSession>.IsFailed(
                    "ACT_SES_03",
                    nameof(ValidateSessionToken),
                    "Sesión temporal sin persona válida.",
                    401,
                    default!);
            }

            return OperationResult<ValidatedSession>.Ok(
                new ValidatedSession { PersonId = personId.Value, Purpose = SessionPurpose },
                nameof(ValidateSessionToken));
        }
        catch (SecurityTokenExpiredException)
        {
            return OperationResult<ValidatedSession>.IsFailed(
                "ACT_SES_04",
                nameof(ValidateSessionToken),
                "La sesión temporal expiró.",
                401,
                default!);
        }
        catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
        {
            return OperationResult<ValidatedSession>.IsFailed(
                "ACT_SES_05",
                nameof(ValidateSessionToken),
                "Sesión temporal inválida.",
                401,
                default!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorInesperadoLog, nameof(ValidateSessionToken));
            return OperationResult<ValidatedSession>.IsFailed(
                "ACT_SES_99",
                nameof(ValidateSessionToken),
                "Error al validar sesión temporal.",
                500,
                default!);
        }
    }

    /// <summary>Genera un JWT de activación/sesión con el subject, propósito y expiración indicados.</summary>
    private static string GenerateToken(string sub, string purpose, TimeSpan duration)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, sub),
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

    /// <summary>Genera un JWT donde el subject es un personId (long).</summary>
    private static string GeneratePersonToken(long personId, string purpose, TimeSpan duration)
        => GenerateToken(personId.ToString(CultureInfo.InvariantCulture), purpose, duration);

    /// <summary>Genera un JWT donde el subject es un flowId (string GUID).</summary>
    public string GenerateFlowIdToken(string flowId, string purpose, TimeSpan duration)
        => GenerateToken(flowId, purpose, duration);

    private static ClaimsPrincipal ValidateJwt(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey())),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        return new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
    }

    private static long? GetPersonId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var personId)
            ? personId
            : null;
    }

    private static bool IsValidPasswordLinkPurpose(string? purpose)
    {
        return string.Equals(purpose, ActivationPurpose, StringComparison.Ordinal)
            || string.Equals(purpose, RecoveryPurpose, StringComparison.Ordinal);
    }

    private static string GetSecretKey()
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

    private TimeSpan GetExpirationHours()
    {
        var hours = _configuration.GetValue<double?>("PasswordActivation:ExpireHours") ?? 24;
        return TimeSpan.FromHours(hours);
    }

    private double GetSessionMinutes()
    {
        return _configuration.GetValue<double?>("PasswordActivation:SessionMinutes") ?? 15;
    }

    internal static string HashToken(string token)
    {
        return TokenHashing.HashSha256Base64(token);
    }
}
