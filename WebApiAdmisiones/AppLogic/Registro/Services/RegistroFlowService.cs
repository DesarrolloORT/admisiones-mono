using AppLogic.Registro.Dtos;
using AppLogic.Registro.Constants;
using AppLogic.Registro.Interfaces;
using AppLogic.Personas.Services;
using System.Text.Json;
using AppLogic.Common.Security;
using AppLogic.Common.Serialization;
using AppLogic.Common.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Utilities;
using AppLogic.Autenticacion.Interfaces;
using AppLogic.Autenticacion.Services;

namespace AppLogic.Registro.Services;

/// <summary>
/// Orquesta el flujo de registro: FlowId session, nueva persona (Redis-deferred), persona existente.
/// </summary>
public class RegistroFlowService : IRegistroFlowService
{
    private const string NuevaPersonaPurpose = "nueva-persona-activacion";
    private const string FlowSessionKeyPrefix = "registro:flow-session:";
    private const string PendingPersonaKeyPrefix = "registro:pending:";
    private const string PendingPersonaDocumentoKeyPrefix = "registro:pending-doc:";

    private readonly IRegistroService _registroService;
    private readonly IPasswordActivationService _passwordActivationService;
    private readonly IConfiguration _configuration;
    private readonly IDatabase _redisDb;
    private readonly IRegistroDocumentoImagenCacheService _documentoImagenCacheService;
    private readonly ILogger<RegistroFlowService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializationDefaults.Redis;

    public RegistroFlowService(
        IRegistroService registroService,
        IPasswordActivationService passwordActivationService,
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        IRegistroDocumentoImagenCacheService documentoImagenCacheService,
        ILogger<RegistroFlowService> logger)
    {
        _registroService = registroService;
        _passwordActivationService = passwordActivationService;
        _configuration = configuration;
        _redisDb = redis.GetDatabase();
        _documentoImagenCacheService = documentoImagenCacheService;
        _logger = logger;
    }

    // ───── FlowSession ─────────────────────────────────────────────────────

    public async Task<string> CrearFlowSessionAsync(string tipoDocumento, string documento, long? codigoPersona)
    {
        var flowId = Guid.NewGuid().ToString("N");
        var session = new DtoRegistroFlowSession
        {
            FlowId = flowId,
            TipoDocumento = tipoDocumento,
            Documento = documento,
            CodigoPersona = codigoPersona,
            Step = RegistroFlowConstants.Step.Evaluado,
            CreatedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(session, JsonOptions);
        var ttl = TimeSpan.FromMinutes(_configuration.GetValue<double?>("Registro:FlowSessionMinutes") ?? 30);
        await _redisDb.StringSetAsync($"{FlowSessionKeyPrefix}{flowId}", json, ttl);

        return flowId;
    }

    public async Task<OperationResult<object?>?> ValidarFlowSessionAsync(string? flowId, string stepEsperado)
    {
        if (string.IsNullOrWhiteSpace(flowId))
        {
            return OperationResult<object?>.IsFailed(
                "FLOW_01",
                nameof(ValidarFlowSessionAsync),
                "El header X-Flow-Id es obligatorio.",
                400);
        }

        var json = await _redisDb.StringGetAsync($"{FlowSessionKeyPrefix}{flowId}");
        if (!json.HasValue)
        {
            return OperationResult<object?>.IsFailed(
                "FLOW_02",
                nameof(ValidarFlowSessionAsync),
                "La sesión de registro expiró o no existe. Reiniciá el proceso desde EvaluarDocumento.",
                400);
        }

        var session = JsonSerializationHelper.TryDeserialize<DtoRegistroFlowSession>(json.ToString(), JsonOptions);

        if (session == null)
        {
            return OperationResult<object?>.IsFailed(
                "FLOW_03",
                nameof(ValidarFlowSessionAsync),
                "Sesión de registro inválida.",
                400);
        }

        if (!string.IsNullOrWhiteSpace(stepEsperado) &&
            !string.Equals(session.Step, stepEsperado, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<object?>.IsFailed(
                "FLOW_04",
                nameof(ValidarFlowSessionAsync),
                $"Paso de registro inválido. Se esperaba '{stepEsperado}' pero el flujo está en '{session.Step}'.",
                400);
        }

        return null; // valid
    }

    private async Task<DtoRegistroFlowSession?> ObtenerFlowSessionAsync(string flowId)
    {
        var json = await _redisDb.StringGetAsync($"{FlowSessionKeyPrefix}{flowId}");
        if (!json.HasValue) return null;

        return JsonSerializationHelper.TryDeserialize<DtoRegistroFlowSession>(json.ToString(), JsonOptions);
    }

    private async Task<OperationResult<RegistroFlowResult>?> ValidarDocumentoFlowResultAsync(
        string flowId,
        string tipoDocumento,
        string documento,
        string originMethod)
    {
        var session = await ObtenerFlowSessionAsync(flowId);
        if (session == null)
        {
            return OperationResult<RegistroFlowResult>.IsFailed(
                "FLOW_03",
                originMethod,
                "Sesion de registro invalida.",
                400,
                default!);
        }

        if (DocumentUtils.Normalizar(session.TipoDocumento) != DocumentUtils.Normalizar(tipoDocumento) ||
            DocumentUtils.Normalizar(session.Documento) != DocumentUtils.Normalizar(documento))
        {
            return OperationResult<RegistroFlowResult>.IsFailed(
                "FLOW_05",
                originMethod,
                "El documento del request no coincide con la sesion de registro.",
                400,
                default!);
        }

        return null;
    }

    public async Task<OperationResult<object?>?> ValidarDocumentoFlowAsync(
        string flowId,
        string tipoDocumento,
        string documento,
        string originMethod)
    {
        var result = await ValidarDocumentoFlowResultAsync(flowId, tipoDocumento, documento, originMethod);
        if (result == null) return null;

        return OperationResult<object?>.IsFailed(
            result.ErrorCode,
            originMethod,
            result.Message,
            result.HttpCode);
    }

    public async Task ActualizarStepAsync(string flowId, string nuevoStep)
    {
        var key = $"{FlowSessionKeyPrefix}{flowId}";
        var json = await _redisDb.StringGetAsync(key);
        if (!json.HasValue) return;

        var session = JsonSerializationHelper.TryDeserialize<DtoRegistroFlowSession>(json.ToString(), JsonOptions);
        if (session == null) return;

        session.Step = nuevoStep;
        var ttl = await _redisDb.KeyTimeToLiveAsync(key);
        await _redisDb.StringSetAsync(key, JsonSerializer.Serialize(session, JsonOptions), ttl);
    }

    public Task EliminarFlowSessionAsync(string flowId)
        => _redisDb.KeyDeleteAsync($"{FlowSessionKeyPrefix}{flowId}");

    // ───── ConfirmarNuevaPersona ────────────────────────────────────────────

    public async Task<OperationResult<RegistroFlowResult>> ConfirmarNuevaPersonaAsync(
        DtoRegistroPersonaRequest request,
        string flowId)
    {
        // 1. Validar solo (sin crear nada en DB)
        var validacion = await _registroService.ValidarNuevaPersonaAsync(request);
        if (!validacion.Success)
        {
            return OperationResult<RegistroFlowResult>.IsFailed(
                validacion.ErrorCode,
                nameof(ConfirmarNuevaPersonaAsync),
                validacion.Message,
                validacion.HttpCode,
                default!);
        }

        var flowDocumentoValidation = await ValidarDocumentoFlowResultAsync(
            flowId,
            request.TipoDocumento,
            request.Documento,
            nameof(ConfirmarNuevaPersonaAsync));
        if (flowDocumentoValidation != null)
        {
            return flowDocumentoValidation;
        }

        // 2. Generar JWT de activación (sub = flow pendiente vigente)
        var expireHours = _configuration.GetValue<double?>("PasswordActivation:ExpireHours") ?? 24;
        var ttl = TimeSpan.FromHours(expireHours);
        var flowIdPending = await ResolverFlowIdPendingDocumentoAsync(
            request.TipoDocumento,
            request.Documento,
            flowId);
        var token = PasswordActivationService.GenerarTokenFlowIdPublic(flowIdPending, NuevaPersonaPurpose, ttl);
        var tokenHash = TokenHashHelper.HashSha256Base64(token);

        // 3. Guardar persona pendiente en Redis
        var pending = new DtoRegistroPendingPersona
        {
            FlowId = flowIdPending,
            TipoDocumento = request.TipoDocumento,
            Documento = request.Documento,
            PrimerApellido = request.PrimerApellido,
            SegundoApellido = request.SegundoApellido,
            PrimerNombre = request.PrimerNombre,
            SegundoNombre = request.SegundoNombre,
            FechaNacimiento = request.FechaNacimiento,
            Sexo = request.Sexo,
            Direccion = request.Direccion,
            Telefono1 = request.Telefono1,
            Email = request.Mail,
            CodigoPais = request.CodigoPais,
            CodigoEstado = request.CodigoEstado,
            CodigoCiudad = request.CodigoCiudad,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow
        };

        var pendingJson = JsonSerializer.Serialize(pending, JsonOptions);
        await _redisDb.StringSetAsync($"{PendingPersonaKeyPrefix}{flowIdPending}", pendingJson, ttl);
        await _redisDb.StringSetAsync(
            CrearPendingDocumentoKey(request.TipoDocumento, request.Documento),
            flowIdPending,
            ttl);

        // 4. Enviar mail de activación
        var mailResult = await _passwordActivationService.EnviarMailNuevaPersonaAsync(flowIdPending, request.Mail, token);
        if (!mailResult.Success)
        {
            // El registro quedó en Redis; el usuario puede reintentar con la opción de reenvío.
            _logger.LogWarning(
                "No se pudo enviar el mail de activación para el flowId {FlowId}: {ErrorCode}.",
                flowIdPending,
                mailResult.ErrorCode);

            return OperationResult<RegistroFlowResult>.Ok(
                new RegistroFlowResult(
                    "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de contraseña.",
                    MailEnviado: false),
                nameof(ConfirmarNuevaPersonaAsync));
        }

        // 5. Actualizar step
        await ActualizarStepAsync(flowId, RegistroFlowConstants.Step.Confirmado);
        if (!string.Equals(flowIdPending, flowId, StringComparison.Ordinal))
        {
            await ActualizarStepAsync(flowIdPending, RegistroFlowConstants.Step.Confirmado);
        }

        return OperationResult<RegistroFlowResult>.Ok(
            new RegistroFlowResult("Registro realizado correctamente. Revisá tu casilla de mail para activar tu contraseña."),
            nameof(ConfirmarNuevaPersonaAsync));
    }

    // ───── PendingPersona ──────────────────────────────────────────────────

    public Task<DtoRegistroPendingPersona?> GetPendingPersonaAsync(string flowId)
        => GetPendingPersonaInternalAsync(flowId);

    private async Task<DtoRegistroPendingPersona?> GetPendingPersonaInternalAsync(string flowId)
    {
        var json = await _redisDb.StringGetAsync($"{PendingPersonaKeyPrefix}{flowId}");
        if (!json.HasValue) return null;

        return JsonSerializationHelper.TryDeserialize<DtoRegistroPendingPersona>(json.ToString(), JsonOptions);
    }

    public async Task DeletePendingPersonaAsync(string flowId)
    {
        var pending = await GetPendingPersonaInternalAsync(flowId);
        if (pending != null)
        {
            await _redisDb.KeyDeleteAsync(CrearPendingDocumentoKey(pending.TipoDocumento, pending.Documento));
        }

        await _redisDb.KeyDeleteAsync($"{PendingPersonaKeyPrefix}{flowId}");
    }

    public Task<OperationResult<long>> CompletarNuevaPersona(DtoRegistroPendingPersona data, string passwordNueva)
        => CompletarNuevaPersonaAsync(data, passwordNueva);

    private async Task<OperationResult<long>> CompletarNuevaPersonaAsync(
        DtoRegistroPendingPersona data,
        string passwordNueva)
    {
        var imagenes = await ObtenerImagenesTemporalesAsync(data);
        var result = await _registroService.CompletarNuevaPersonaAsync(data, passwordNueva, imagenes);

        if (result.Success)
        {
            await EliminarImagenesTemporalesAsync(data);
        }

        return result;
    }

    private async Task<DtoRegistroDocumentoImagenesTemporales?> ObtenerImagenesTemporalesAsync(
        DtoRegistroPendingPersona data)
    {
        return await DocumentoIdentidadPersonaService.ObtenerImagenesTemporalesSeguroAsync(
            _documentoImagenCacheService,
            data.TipoDocumento,
            data.Documento,
            _logger);
    }

    private async Task EliminarImagenesTemporalesAsync(DtoRegistroPendingPersona data)
    {
        await DocumentoIdentidadPersonaService.EliminarImagenesTemporalesSeguroAsync(
            _documentoImagenCacheService,
            data.TipoDocumento,
            data.Documento,
            _logger);
    }

    private async Task<string> ResolverFlowIdPendingDocumentoAsync(
        string tipoDocumento,
        string documento,
        string fallbackFlowId)
    {
        var docKey = CrearPendingDocumentoKey(tipoDocumento, documento);
        var existingFlowId = await _redisDb.StringGetAsync(docKey);
        if (!existingFlowId.HasValue)
        {
            return fallbackFlowId;
        }

        var flowId = existingFlowId.ToString();
        if (string.IsNullOrWhiteSpace(flowId))
        {
            await _redisDb.KeyDeleteAsync(docKey);
            return fallbackFlowId;
        }

        var pending = await GetPendingPersonaInternalAsync(flowId);
        if (pending != null)
        {
            return flowId;
        }

        await _redisDb.KeyDeleteAsync(docKey);
        return fallbackFlowId;
    }

    private static string CrearPendingDocumentoKey(string tipoDocumento, string documento)
    {
        var tipoNormalizado = DocumentUtils.NormalizarMayusculas(tipoDocumento);
        var documentoNormalizado = DocumentUtils.NormalizarMayusculas(documento);
        if (DocumentUtils.EsCedula(tipoNormalizado))
        {
            documentoNormalizado = new string(documentoNormalizado.Where(char.IsDigit).ToArray());
        }

        return $"{PendingPersonaDocumentoKeyPrefix}{tipoNormalizado}:{documentoNormalizado}";
    }
}
