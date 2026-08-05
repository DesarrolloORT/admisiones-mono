using AppLogic.Contracts;
using AppLogic.Authentication.Contracts;
using AppLogic.Authentication.Dtos;
using AppLogic.Authentication.Interfaces;
using AppLogic.Authentication.Services;
using AppLogic.Identity.Dtos;
using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IServices;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Utilities;

namespace AppLogic.Authentication.UseCases;

/// <summary>
/// Alta de la contraseña inicial. La sesión temporal decide el camino: persona nueva (los datos
/// están en Redis y los completa el módulo de registro) o persona ya existente en el padrón.
/// </summary>
public class CompletePasswordFlow(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext,
    ILdap ldap,
    IPasswordActivationService passwordActivationService,
    IHashTokenStore hashTokenStore,
    IPendingRegistrationCompletion pendingRegistration,
    IIdentityDocumentImageCache documentImageCache,
    IRefreshTokenService refreshTokenService,
    SessionTokenIssuer tokenIssuer,
    IIssueTokensForPerson issueTokensForPerson,
    ILogger<CompletePasswordFlow> logger) : ICompletePasswordFlow
{
    private const string NewPersonSessionPurpose = "nueva-persona-session";

    // Se preserva el literal "CompleteInitialPassword" (nombre del método del controller antes de la
    // extracción a servicio) para no cambiar el campo Method del OperationResult devuelto al front.
    private const string OriginMethod = "CompleteInitialPassword";

    // Se preserva el literal "CompletarPasswordAsync" (nombre original del método antes del rename)
    // para no cambiar el campo Method del OperationResult devuelto al front.
    private const string ExistingPersonOriginMethod = "CompletarPasswordAsync";

    private const string TokensMessage =
        "Contraseña creada correctamente. Los tokens han sido establecidos como cookies seguras.";

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;
    private readonly ILdap _ldap = ldap;
    private readonly IPasswordActivationService _passwordActivationService = passwordActivationService;
    private readonly IHashTokenStore _hashTokenStore = hashTokenStore;
    private readonly IPendingRegistrationCompletion _pendingRegistration = pendingRegistration;
    private readonly IIdentityDocumentImageCache _documentImageCache = documentImageCache;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly SessionTokenIssuer _tokenIssuer = tokenIssuer;
    private readonly IIssueTokensForPerson _issueTokensForPerson = issueTokensForPerson;
    private readonly ILogger<CompletePasswordFlow> _logger = logger;

    public async Task<CompletePasswordFlowResult> ExecuteAsync(
        string? sessionToken,
        CompleteInitialPasswordRequest request)
    {
        var sessionResult = _passwordActivationService.ValidateSessionToken(sessionToken ?? string.Empty);

        if (!sessionResult.Success)
        {
            return new CompletePasswordFlowResult
            {
                ClearActivationCookie = true,
                Result = sessionResult.Failure().As<AuthenticationResponse>(OriginMethod)
            };
        }

        var session = sessionResult.Data;
        if (session == null)
        {
            return Rejected("ACT_SES_06", "Sesion temporal invalida.");
        }

        if (string.Equals(session.Purpose, NewPersonSessionPurpose, StringComparison.Ordinal))
        {
            return await CompleteNewPersonAsync(session, request);
        }

        return await CompleteExistingPersonAsync(session, request);
    }

    private async Task<CompletePasswordFlowResult> CompleteNewPersonAsync(
        ValidatedSession session,
        CompleteInitialPasswordRequest request)
    {
        var flowId = session.FlowId;
        if (string.IsNullOrWhiteSpace(flowId))
        {
            return Rejected("ACT_SES_NUP_01", "Sesion temporal sin FlowId valido.");
        }

        var pending = await _pendingRegistration.GetPendingPersonAsync(flowId);
        if (pending == null)
        {
            return Rejected(
                "NUP_COMP_01",
                "El registro pendiente expiró o ya fue completado. Por favor, iniciá el proceso de registro nuevamente.");
        }

        var createResult = await _pendingRegistration.CreatePersonFromPendingAsync(pending, request.NewPassword);
        if (!createResult.Success)
        {
            return new CompletePasswordFlowResult
            {
                ClearActivationCookie = false,
                Result = createResult.Failure().As<AuthenticationResponse>(OriginMethod)
            };
        }

        var tokenResult = await _issueTokensForPerson.ExecuteAsync(createResult.Data, TokensMessage);
        var completed = tokenResult.Success && tokenResult.Data != null;
        if (completed)
        {
            await _pendingRegistration.DeletePendingPersonAsync(flowId);
            await _pendingRegistration.DeleteFlowSessionAsync(flowId);
        }

        return new CompletePasswordFlowResult
        {
            ClearActivationCookie = completed,
            Result = tokenResult
        };
    }

    private async Task<CompletePasswordFlowResult> CompleteExistingPersonAsync(
        ValidatedSession session,
        CompleteInitialPasswordRequest request)
    {
        if (!session.PersonId.HasValue)
        {
            return Rejected("ACT_SES_03", "Sesion temporal sin persona valida.");
        }

        var result = await SetPasswordForExistingPersonAsync(session.PersonId.Value, request);

        return new CompletePasswordFlowResult
        {
            ClearActivationCookie = result.Success && result.Data != null,
            Result = result
        };
    }

    private async Task<OperationResult<AuthenticationResponse>> SetPasswordForExistingPersonAsync(
        long personId,
        CompleteInitialPasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return Failed("INI_PAS_01", "La solicitud es obligatoria.", 400);
            }

            var passwordValidation = Util.ValidarPasswordNueva(request.NewPassword);
            if (!string.IsNullOrWhiteSpace(passwordValidation))
            {
                return Failed("INI_PAS_02", passwordValidation, 400);
            }

            using var uow = _uowFactory.Create();
            var person = uow.Personas.GetByKey(personId);

            if (person == null)
            {
                return Failed("INI_PAS_03", "Usuario no encontrado en la base de datos.", 404);
            }

            var storedHash = await _hashTokenStore.GetAsync(personId.ToString(CultureInfo.InvariantCulture));
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return Failed("INI_PAS_04", "El link de activación ya fue utilizado o no está vigente.", 401);
            }

            var images = await GetTemporaryImagesAsync(person);
            var imagesValidation = IdentityDocumentService.ValidateRecognizedDocumentImages(
                images,
                ExistingPersonOriginMethod);
            if (!imagesValidation.Success)
            {
                return imagesValidation.Failure().As<AuthenticationResponse>(ExistingPersonOriginMethod);
            }

            var passwordChange = await _ldap.ForzarCambiarPasswordAsync(
                personId.ToString(CultureInfo.InvariantCulture),
                request.NewPassword);

            if (!passwordChange.Success)
            {
                return passwordChange.Failure().As<AuthenticationResponse>(ExistingPersonOriginMethod);
            }

            person.FechaUltModifPassword = DateTime.Today;
            person.UsuarioUltModifPassword = "ADMISIONES";
            SaveRecognizedDocumentImages(uow, person, images);
            uow.Personas.Update(person);

            try
            {
                uow.Save();
            }
            catch (Exception ex)
            {
                // La password ya cambió en LDAP (irreversible); el link de activación sigue
                // vigente para que el usuario reintente en vez de quedar en un estado sin salida.
                _logger.LogError(ex,
                    "Estado inconsistente: password de la persona {CodigoPersona} ya cambiada en LDAP pero no persistida en DB.",
                    personId);
                throw;
            }

            // El link de activación se consume solo después de confirmar la persistencia en DB.
            await _hashTokenStore.DeleteAsync(personId.ToString(CultureInfo.InvariantCulture));
            await DeleteTemporaryImagesAsync(person, images);

            var authResponse = await _tokenIssuer.IssueAsync(
                person,
                personId,
                _refreshTokenService,
                TokensMessage);

            return OperationResult<AuthenticationResponse>.Ok(authResponse, ExistingPersonOriginMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AuthenticationLogs.UnexpectedError, ExistingPersonOriginMethod);
            return Failed("INI_PAS_99", "Error al completar password inicial.", 500);
        }
    }

    private Task<TemporaryDocumentImages?> GetTemporaryImagesAsync(Persona person) =>
        IdentityDocumentService.GetTemporaryImagesSafeAsync(
            _documentImageCache,
            person.TipoDocumento,
            person.Documento,
            _logger);

    private async Task DeleteTemporaryImagesAsync(Persona person, TemporaryDocumentImages? images)
    {
        if (images is null)
        {
            return;
        }

        await IdentityDocumentService.DeleteTemporaryImagesSafeAsync(
            _documentImageCache,
            person.TipoDocumento,
            person.Documento,
            _logger);
    }

    private void SaveRecognizedDocumentImages(IUnitOfWork uow, Persona person, TemporaryDocumentImages? images)
    {
        if (images is null)
        {
            return;
        }

        IdentityDocumentService.SaveRecognizedDocumentImages(uow, _dbConnectionContext, person, images);
    }

    /// <summary>Sesión temporal inservible: se responde 401 y se limpia la cookie de activación.</summary>
    private static CompletePasswordFlowResult Rejected(string errorCode, string message) => new()
    {
        ClearActivationCookie = true,
        Result = OperationResult<AuthenticationResponse>.IsFailed(errorCode, OriginMethod, message, 401, default!)
    };

    private static OperationResult<AuthenticationResponse> Failed(string errorCode, string message, int httpCode) =>
        OperationResult<AuthenticationResponse>.IsFailed(errorCode, ExistingPersonOriginMethod, message, httpCode, default!);
}
