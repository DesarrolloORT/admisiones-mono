using AppLogic.Registration.Mapping;
using AppLogic.Identity.Services;
using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.Contracts.Text;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Constants;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AppLogic.Authentication.Security;
using AppLogic.Platform.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using AppLogic.Contracts;
using Utilities;
using AppLogic.Authentication.Interfaces;

namespace AppLogic.Registration.Services;

/// <summary>
/// Orquesta el flujo de registro: FlowId session, nueva persona (Redis-deferred), persona existente.
/// </summary>
public class RegistrationFlowService : IRegistrationFlowService
{
    private const string NuevaPersonaPurpose = "nueva-persona-activacion";
    private const string FlowSessionKeyPrefix = "registro:flow-session:";

    private readonly IValidateNewPerson _validateNewPerson;
    private readonly ICompleteNewPerson _completeNewPerson;
    private readonly IPasswordActivationService _passwordActivationService;
    private readonly IConfiguration _configuration;
    private readonly IDatabase _redisDb;
    private readonly IIdentityDocumentImageCache _documentoImagenCacheService;
    private readonly IPendingPersonStore _pendingPersonaStore;
    private readonly ILogger<RegistrationFlowService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializationDefaults.Redis;

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "Orquesta el flujo de registro completo: validación, Redis, cache de imágenes y mail. " +
                        "Las dependencias son las de los pasos del flujo, no acumulación accidental.")]
    public RegistrationFlowService(
        IValidateNewPerson validateNewPerson,
        ICompleteNewPerson completeNewPerson,
        IPasswordActivationService passwordActivationService,
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        IIdentityDocumentImageCache documentoImagenCacheService,
        IPendingPersonStore pendingPersonaStore,
        ILogger<RegistrationFlowService> logger)
    {
        _validateNewPerson = validateNewPerson;
        _completeNewPerson = completeNewPerson;
        _passwordActivationService = passwordActivationService;
        _configuration = configuration;
        _redisDb = redis.GetDatabase();
        _documentoImagenCacheService = documentoImagenCacheService;
        _pendingPersonaStore = pendingPersonaStore;
        _logger = logger;
    }

    // ───── FlowSession ─────────────────────────────────────────────────────

    public async Task<string> CreateFlowSessionAsync(string documentType, string document, long? personId)
    {
        var flowId = Guid.NewGuid().ToString("N");
        var session = new RegistrationFlowSession
        {
            FlowId = flowId,
            DocumentType = documentType,
            DocumentNumber = document,
            PersonId = personId,
            Step = RegistrationFlowConstants.Step.Evaluado,
            CreatedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(session, JsonOptions);
        var ttl = TimeSpan.FromMinutes(_configuration.GetValue<double?>("Registro:FlowSessionMinutes") ?? 30);
        await _redisDb.StringSetAsync($"{FlowSessionKeyPrefix}{flowId}", json, ttl);

        return flowId;
    }

    public async Task<OperationResult<object?>?> ValidateFlowSessionAsync(string? flowId, string stepEsperado)
    {
        if (string.IsNullOrWhiteSpace(flowId))
        {
            return OperationResult<object?>.IsFailed(
                "FLOW_01",
                nameof(ValidateFlowSessionAsync),
                "El header X-Flow-Id es obligatorio.",
                400);
        }

        var json = await _redisDb.StringGetAsync($"{FlowSessionKeyPrefix}{flowId}");
        if (!json.HasValue)
        {
            return OperationResult<object?>.IsFailed(
                "FLOW_02",
                nameof(ValidateFlowSessionAsync),
                "La sesión de registro expiró o no existe. Reiniciá el proceso desde EvaluateDocument.",
                400);
        }

        var session = JsonSerialization.TryDeserialize<RegistrationFlowSession>(json.ToString(), JsonOptions);

        if (session == null)
        {
            return OperationResult<object?>.IsFailed(
                "FLOW_03",
                nameof(ValidateFlowSessionAsync),
                "Sesión de registro inválida.",
                400);
        }

        if (!string.IsNullOrWhiteSpace(stepEsperado) &&
            !string.Equals(session.Step, stepEsperado, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<object?>.IsFailed(
                "FLOW_04",
                nameof(ValidateFlowSessionAsync),
                $"Paso de registro inválido. Se esperaba '{stepEsperado}' pero el flujo está en '{session.Step}'.",
                400);
        }

        return null; // valid
    }

    private async Task<RegistrationFlowSession?> GetFlowSessionAsync(string flowId)
    {
        var json = await _redisDb.StringGetAsync($"{FlowSessionKeyPrefix}{flowId}");
        if (!json.HasValue) return null;

        return JsonSerialization.TryDeserialize<RegistrationFlowSession>(json.ToString(), JsonOptions);
    }

    private async Task<OperationResult<RegistrationFlowResult>?> ValidateFlowDocumentResultAsync(
        string flowId,
        string? documentType,
        string? document,
        string originMethod)
    {
        var session = await GetFlowSessionAsync(flowId);
        if (session == null)
        {
            return OperationResult<RegistrationFlowResult>.IsFailed(
                "FLOW_03",
                originMethod,
                "Sesion de registro invalida.",
                400,
                default!);
        }

        if (TextNormalization.Trim(session.DocumentType) != TextNormalization.Trim(documentType) ||
            TextNormalization.Trim(session.DocumentNumber) != TextNormalization.Trim(document))
        {
            return OperationResult<RegistrationFlowResult>.IsFailed(
                "FLOW_05",
                originMethod,
                "El documento del request no coincide con la sesion de registro.",
                400,
                default!);
        }

        return null;
    }

    public async Task<OperationResult<object?>?> ValidateFlowDocumentAsync(
        string flowId,
        string? documentType,
        string? document,
        string originMethod)
    {
        var result = await ValidateFlowDocumentResultAsync(flowId, documentType, document, originMethod);
        if (result == null) return null;

        return result.Failure().As<object?>(originMethod);
    }

    public async Task UpdateStepAsync(string flowId, string nuevoStep)
    {
        var key = $"{FlowSessionKeyPrefix}{flowId}";
        var json = await _redisDb.StringGetAsync(key);
        if (!json.HasValue) return;

        var session = JsonSerialization.TryDeserialize<RegistrationFlowSession>(json.ToString(), JsonOptions);
        if (session == null) return;

        session.Step = nuevoStep;
        var ttl = await _redisDb.KeyTimeToLiveAsync(key);
        await _redisDb.StringSetAsync(key, JsonSerializer.Serialize(session, JsonOptions), ttl);
    }

    public Task DeleteFlowSessionAsync(string flowId)
        => _redisDb.KeyDeleteAsync($"{FlowSessionKeyPrefix}{flowId}");

    // ───── ConfirmNewPerson ────────────────────────────────────────────

    public async Task<OperationResult<RegistrationFlowResult>> ConfirmNewPersonAsync(
        RegisterPersonRequest request,
        string flowId)
    {
        // 1. Validar solo (sin crear nada en DB)
        var validation = await _validateNewPerson.ExecuteAsync(request);
        if (!validation.Success)
        {
            return validation.Failure().As<RegistrationFlowResult>(nameof(ConfirmNewPersonAsync));
        }

        var flowDocumentValidation = await ValidateFlowDocumentResultAsync(
            flowId,
            request.DocumentType,
            request.DocumentNumber,
            nameof(ConfirmNewPersonAsync));
        if (flowDocumentValidation != null)
        {
            return flowDocumentValidation;
        }

        // 2. Generar JWT de activación (sub = flow pendiente vigente)
        var expireHours = _configuration.GetValue<double?>("PasswordActivation:ExpireHours") ?? 24;
        var ttl = TimeSpan.FromHours(expireHours);
        var flowIdPending = await ResolvePendingDocumentFlowIdAsync(
            request.DocumentType,
            request.DocumentNumber,
            flowId);
        var token = _passwordActivationService.GenerateFlowIdToken(flowIdPending, NuevaPersonaPurpose, ttl);
        var tokenHash = TokenHashing.HashSha256Base64(token);

        // 3. Guardar persona pendiente en Redis
        var pending = RegistrationMapper.ToPendingPerson(request, flowIdPending, tokenHash);
        await _pendingPersonaStore.SaveAsync(pending, ttl);

        // 4. Enviar mail de activación
        var mailResult = await _passwordActivationService.SendNewPersonMailAsync(flowIdPending, request.Email, token);
        if (!mailResult.Success)
        {
            // El registro quedó en Redis; el usuario puede reintentar con la opción de reenvío.
            _logger.LogWarning(
                "No se pudo enviar el mail de activación para el flowId {FlowId}: {ErrorCode}.",
                flowIdPending,
                mailResult.ErrorCode);

            return OperationResult<RegistrationFlowResult>.Ok(
                new RegistrationFlowResult(
                    "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de contraseña.",
                    MailSent: false),
                nameof(ConfirmNewPersonAsync));
        }

        // 5. Actualizar step
        await UpdateStepAsync(flowId, RegistrationFlowConstants.Step.Confirmado);
        if (!string.Equals(flowIdPending, flowId, StringComparison.Ordinal))
        {
            await UpdateStepAsync(flowIdPending, RegistrationFlowConstants.Step.Confirmado);
        }

        return OperationResult<RegistrationFlowResult>.Ok(
            new RegistrationFlowResult("Registro realizado correctamente. Revisá tu casilla de mail para activar tu contraseña."),
            nameof(ConfirmNewPersonAsync));
    }

    // ───── PendingPersona ──────────────────────────────────────────────────

    public Task<PendingPerson?> GetPendingPersonAsync(string flowId)
        => _pendingPersonaStore.GetAsync(flowId);

    public Task DeletePendingPersonAsync(string flowId)
        => _pendingPersonaStore.DeleteAsync(flowId);

    public Task<OperationResult<long>> CreatePersonFromPendingAsync(PendingPerson data, string newPassword)
        => CompleteNewPersonAsync(data, newPassword);

    private async Task<OperationResult<long>> CompleteNewPersonAsync(
        PendingPerson data,
        string newPassword)
    {
        var images = await GetTemporaryImagesAsync(data);
        var result = await _completeNewPerson.ExecuteAsync(data, newPassword, images);

        if (result.Success)
        {
            await DeleteTemporaryImagesAsync(data);
        }

        return result;
    }

    private async Task<TemporaryDocumentImages?> GetTemporaryImagesAsync(
        PendingPerson data)
    {
        return await IdentityDocumentService.GetTemporaryImagesSafeAsync(
            _documentoImagenCacheService,
            data.DocumentType,
            data.DocumentNumber,
            _logger);
    }

    private async Task DeleteTemporaryImagesAsync(PendingPerson data)
    {
        await IdentityDocumentService.DeleteTemporaryImagesSafeAsync(
            _documentoImagenCacheService,
            data.DocumentType,
            data.DocumentNumber,
            _logger);
    }

    private async Task<string> ResolvePendingDocumentFlowIdAsync(
        string documentType,
        string document,
        string fallbackFlowId)
    {
        var flowId = await _pendingPersonaStore.ResolveFlowIdByDocumentAsync(documentType, document);
        return flowId ?? fallbackFlowId;
    }
}
