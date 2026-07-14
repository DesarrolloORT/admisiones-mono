using AppLogic.Autenticacion.Requests;
using AppLogic.Autenticacion.Responses;
using AppLogic.Autenticacion.Dtos;
using AppLogic.Autenticacion.Helpers;
using AppLogic.Autenticacion.Interfaces;
using AppLogic.Common.Security;
using AppLogic.Registro.Dtos;
using AppLogic.Registro.Interfaces;
using AppLogic.Personas.Services;
using AppLogic.Common.Validation;
using BusinessLogic.Entities;
using BusinessLogic.IServices;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Utilities;

namespace AppLogic.Autenticacion.Services;

public class AuthService : IAuthService
{
    private const string MensajeGenericoRecupero =
        "Si los datos ingresados son correctos, recibiras un mail con instrucciones para recuperar tu contraseña.";
    private const string SISTEMA = "ADMISIONESWEB";
    private const string ErrorInesperadoLog = "Error inesperado en {Metodo}";
    private const string NuevaPersonaSessionPurpose = "nueva-persona-session";

    // Se preserva el literal "CompletarPassword" (nombre del método del controller antes de esta
    // extracción) para no cambiar el campo Method del OperationResult devuelto al front.
    private const string CompletarPasswordOriginMethod = "CompletarPassword";

    // Se preserva el literal "CompletarPasswordAsync" (nombre original del método antes de este
    // rename) para no cambiar el campo Method del OperationResult devuelto al front.
    private const string CompletarPasswordPersonaExistenteOriginMethod = "CompletarPasswordAsync";

    private readonly ILdap _ldap;
    private readonly BusinessLogic.IDevartRepositories.IUnitOfWorkFactory _admisionesUowFactory;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPasswordActivationService _passwordActivationService;
    private readonly IHashTokenStore _hashTokenStore;
    private readonly IRegistroFlowService _registroFlowService;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private readonly IDbConnectionContext? _dbConnectionContext;
    private readonly IRegistroDocumentoImagenCacheService? _documentoImagenCacheService;
    private readonly ILogger<AuthService>? _logger;

    /// <summary>
    /// Constructor del servicio LDAP.
    /// </summary>
    /// <param name="ldap">Servicio de autenticación LDAP (Core/Autenticacion).</param>
    /// <param name="admisionesUowFactory">Factory para crear unidades de trabajo (Personas).</param>
    /// <param name="tokenService">Servicio para la generación de tokens JWT.</param>
    /// <param name="refreshTokenService">Servicio para gestionar refresh tokens en la base de datos.</param>
    public AuthService(
        ILdap ldap,
        BusinessLogic.IDevartRepositories.IUnitOfWorkFactory admisionesUowFactory,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IPasswordActivationService passwordActivationService,
        IHashTokenStore hashTokenStore,
        IRegistroFlowService registroFlowService,
        IServiceScopeFactory? serviceScopeFactory = null,
        IDbConnectionContext? dbConnectionContext = null,
        IRegistroDocumentoImagenCacheService? documentoImagenCacheService = null,
        ILogger<AuthService>? logger = null)
    {
        _ldap = ldap;
        _admisionesUowFactory = admisionesUowFactory;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _passwordActivationService = passwordActivationService;
        _hashTokenStore = hashTokenStore;
        _registroFlowService = registroFlowService;
        _serviceScopeFactory = serviceScopeFactory;
        _dbConnectionContext = dbConnectionContext;
        _documentoImagenCacheService = documentoImagenCacheService;
        _logger = logger;
    }

    /// <summary>
    /// Autentica un usuario contra el servicio LDAP delegando al proyecto Autenticacion
    /// y, si es exitoso, obtiene la Persona desde la base de datos y genera los tokens de autenticación.
    /// </summary>
    /// <param name="tipoDocumento">Tipo de documento del usuario.</param>
    /// <param name="documento">Número de documento del usuario.</param>
    /// <param name="pass">Contraseña del usuario.</param>
    /// <returns>OperationResult con la respuesta de autenticación incluyendo tokens y la Persona autenticada si el login es exitoso.</returns>
    public async Task<OperationResult<DtoPersonaAuth>> AutenticarUsuarioLDAPAsync(string tipoDocumento, string documento, string pass)
    {
        try
        {
            // Validar el tipo de documento y documento
            var validacion = DocumentUtils.ValidarDocumentoBase(tipoDocumento, documento);
            if (!validacion.IsValid)
            {
                return OperationResult<DtoPersonaAuth>.IsFailed(
                    ObtenerCodigoValidacionDocumentoLogin(validacion.Error),
                    nameof(AutenticarUsuarioLDAPAsync),
                    validacion.Message,
                    400,
                    default!);
            }

            var tipoDocumentoNorm = DocumentUtils.Normalizar(tipoDocumento);
            var documentoNorm = DocumentUtils.Normalizar(documento);

            // Obtener la Persona desde la base de datos por tipo documento y documento
            using var admisionesUow = _admisionesUowFactory.Create();
            var persona = admisionesUow.Personas.GetByTipoDocumentoYDocumento(tipoDocumentoNorm, documentoNorm);

            if (persona == null)
            {
                return OperationResult<DtoPersonaAuth>.IsFailed(
                    "LOGIN_LDAP_04",
                    nameof(AutenticarUsuarioLDAPAsync),
                    "No se encontró la persona en la base de datos.",
                    404,
                    default!);
            }

            // Delegar la autenticación LDAP al servicio de Core/Autenticacion
            var authResult = await _ldap.AutenticarUsuarioLDAPAsync(persona.CodigoPersona, pass);

            if (!authResult.Success)
            {
                return OperationResult<DtoPersonaAuth>.IsFailed(
                    authResult.ErrorCode,
                    nameof(AutenticarUsuarioLDAPAsync),
                    authResult.Message,
                    authResult.HttpCode,
                    default!);
            }

            // No se emiten tokens acá (SEG-03): el llamador decide cuándo, según el gate de 2FA.
            return OperationResult<DtoPersonaAuth>.Ok(
                AuthenticationResponseBuilder.BuildPersonaAuth(persona),
                nameof(AutenticarUsuarioLDAPAsync));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ErrorInesperadoLog, nameof(AutenticarUsuarioLDAPAsync));
            return OperationResult<DtoPersonaAuth>.IsFailed(
                "LOGIN_LDAP_99",
                nameof(AutenticarUsuarioLDAPAsync),
                "Error al autenticar usuario.",
                500,
                default!);
        }
    }

    /// <summary>
    /// Refresca los tokens de autenticación usando el refresh token.
    /// Valida el refresh token contra la base de datos y genera nuevos tokens.
    /// </summary>
    /// <param name="refreshToken">Refresh token enviado por el cliente.</param>
    /// <returns>OperationResult con los nuevos tokens generados.</returns>
    public async Task<OperationResult<DtoAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken)
    {
        try
        {
            // 1. Validar que el refresh token exista
            if (string.IsNullOrEmpty(refreshToken))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_01",
                    nameof(RefrescarTokensAsync),
                    "No se encontró refresh token en las cookies.",
                    401,
                    default!);
            }

            // 2. Resolver código de persona desde un refresh token válido
            var refreshTokenHash = _tokenService.HashToken(refreshToken);
            var codigoPersona = await _refreshTokenService.GetCodigoPersonaByRefreshTokenAsync(
                SISTEMA, refreshTokenHash);

            if (!codigoPersona.HasValue)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_03",
                    nameof(RefrescarTokensAsync),
                    "Refresh token inválido o expirado.",
                    401,
                    default!);
            }

            // 3. Obtener persona de la base de datos
            using var admisionesUow = _admisionesUowFactory.Create();
            var persona = admisionesUow.Personas.GetByKey(codigoPersona.Value);

            if (persona == null)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_04",
                    nameof(RefrescarTokensAsync),
                    "Usuario no encontrado en la base de datos.",
                    404,
                    default!);
            }

            var authResponse = await GenerarYPersistirTokensAsync(
                persona,
                codigoPersona.Value,
                _refreshTokenService,
                "Tokens renovados correctamente.");

            return OperationResult<DtoAuthenticationResponse>.Ok(authResponse, nameof(RefrescarTokensAsync));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ErrorInesperadoLog, nameof(RefrescarTokensAsync));
            return OperationResult<DtoAuthenticationResponse>.IsFailed(
                "REFRESH_TOKEN_99",
                nameof(RefrescarTokensAsync),
                "Error al refrescar tokens.",
                500,
                default!);
        }
    }

    /// <summary>
    /// Gestiona la recuperación de password para usuarios
    /// </summary>
    /// <param name="request">Datos necesarios para identificar a la persona.</param>
    /// <returns>Resultado codificado del proceso de recuperación.</returns>
    public async Task<OperationResult<object>> RecuperarPassword(DtoRecuperarPasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return OperationResult<object>.IsFailed(
                    "REC_PAS_01",
                    nameof(RecuperarPassword),
                    "La solicitud es obligatoria.",
                    400);
            }

            var validacion = DocumentUtils.ValidarDocumentoBase(request.TipoDocumento, request.Documento);
            if (!validacion.IsValid)
            {
                return OperationResult<object>.IsFailed(
                    ObtenerCodigoValidacionDocumentoRecuperarPassword(validacion.Error),
                    nameof(RecuperarPassword),
                    validacion.Message,
                    400);
            }

            var uow = _admisionesUowFactory.Create();
            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            var documento = DocumentUtils.Normalizar(request.Documento);
            var persona = uow.Personas.GetByDocumento(documento);

            if (persona == null || !CoincidePersonaRecupero(persona, tipoDocumento, documento, request.PrimerApellido))
            {
                return OperationResult<object>.IsSuccess(
                    null,
                    nameof(RecuperarPassword),
                    MensajeGenericoRecupero);
            }

            var envioMail = await _passwordActivationService.EnviarMailRecuperacionPasswordAsync(
                persona,
                nameof(RecuperarPassword));

            return OperationResult<object>.IsSuccess(
                null,
                nameof(RecuperarPassword),
                envioMail.Success && !string.IsNullOrWhiteSpace(envioMail.Message)
                    ? envioMail.Message
                    : MensajeGenericoRecupero);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ErrorInesperadoLog, nameof(RecuperarPassword));
            return OperationResult<object>.IsSuccess(
               null,
               nameof(RecuperarPassword),
               MensajeGenericoRecupero);
        }
    }

    /// <summary>
    /// Completa el alta de password inicial usando una sesion temporal de activacion.
    /// </summary>
    /// <param name="codigoPersona">Codigo de persona resuelto desde la sesion temporal.</param>
    /// <param name="request">Nueva password a establecer.</param>
    /// <returns>Respuesta de autenticacion normal con tokens para cookies.</returns>
    private async Task<OperationResult<DtoAuthenticationResponse>> CompletarPasswordPersonaExistenteAsync(
        long codigoPersona,
        DtoCompletarPasswordInicialRequest request)
    {
        try
        {
            if (request == null)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_01",
                    CompletarPasswordPersonaExistenteOriginMethod,
                    "La solicitud es obligatoria.",
                    400,
                    default!);
            }

            var validacionPassword = Util.ValidarPasswordNueva(request.PasswordNueva);
            if (!string.IsNullOrWhiteSpace(validacionPassword))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_02",
                    CompletarPasswordPersonaExistenteOriginMethod,
                    validacionPassword,
                    400,
                    default!);
            }

            using var uow = _admisionesUowFactory.Create();
            var persona = uow.Personas.GetByKey(codigoPersona);

            if (persona == null)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_03",
                    CompletarPasswordPersonaExistenteOriginMethod,
                    "Usuario no encontrado en la base de datos.",
                    404,
                    default!);
            }

            var storedHash = await _hashTokenStore.GetAsync(codigoPersona.ToString(CultureInfo.InvariantCulture));
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_04",
                    CompletarPasswordPersonaExistenteOriginMethod,
                    "El link de activación ya fue utilizado o no está vigente.",
                    401,
                    default!);
            }

            var imagenes = await ObtenerImagenesTemporalesAsync(persona);
            var imagenesValidation = DocumentoIdentidadPersonaService.ValidarImagenesDocumentoReconocido(
                imagenes,
                CompletarPasswordPersonaExistenteOriginMethod);
            if (!imagenesValidation.Success)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    imagenesValidation.ErrorCode,
                    CompletarPasswordPersonaExistenteOriginMethod,
                    imagenesValidation.Message,
                    imagenesValidation.HttpCode,
                    default!);
            }

            var cambioPassword = await _ldap.ForzarCambiarPasswordAsync(
                codigoPersona.ToString(CultureInfo.InvariantCulture),
                request.PasswordNueva);

            if (!cambioPassword.Success)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    cambioPassword.ErrorCode,
                    CompletarPasswordPersonaExistenteOriginMethod,
                    cambioPassword.Message,
                    cambioPassword.HttpCode,
                    default!);
            }

            persona.FechaUltModifPassword = DateTime.Today;
            persona.UsuarioUltModifPassword = "ADMISIONES";
            GuardarImagenesDocumentoReconocido(uow, persona, imagenes);
            uow.Personas.Update(persona);

            try
            {
                uow.Save();
            }
            catch (Exception ex)
            {
                // La password ya cambió en LDAP (irreversible); el link de activación sigue
                // vigente para que el usuario reintente en vez de quedar en un estado sin salida.
                _logger?.LogError(ex,
                    "Estado inconsistente: password de la persona {CodigoPersona} ya cambiada en LDAP pero no persistida en DB.",
                    codigoPersona);
                throw;
            }

            // El link de activación se consume solo después de confirmar la persistencia en DB.
            await _hashTokenStore.DeleteAsync(codigoPersona.ToString(CultureInfo.InvariantCulture));
            await EliminarImagenesTemporalesAsync(persona, imagenes);

            var authResponse = await GenerarYPersistirTokensAsync(
                persona,
                codigoPersona,
                _refreshTokenService,
                "Contraseña creada correctamente. Los tokens han sido establecidos como cookies seguras.");

            return OperationResult<DtoAuthenticationResponse>.Ok(
                authResponse,
                CompletarPasswordPersonaExistenteOriginMethod);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ErrorInesperadoLog, CompletarPasswordPersonaExistenteOriginMethod);
            return OperationResult<DtoAuthenticationResponse>.IsFailed(
                "INI_PAS_99",
                CompletarPasswordPersonaExistenteOriginMethod,
                "Error al completar password inicial.",
                500,
                default!);
        }
    }

    /// <summary>
    /// Orquesta CompletarPassword: valida la sesión temporal y despacha al flujo de persona nueva
    /// (Redis) o persona existente. Movido desde AuthController.CompletarPassword sin cambiar
    /// validaciones, códigos de error ni el campo Method de las respuestas.
    /// </summary>
    public async Task<DtoCompletarPasswordFlowResult> CompletarPasswordFlowAsync(
        string? sessionToken,
        DtoCompletarPasswordInicialRequest request)
    {
        var sessionResult = _passwordActivationService.ValidarSessionToken(sessionToken ?? string.Empty);

        if (!sessionResult.Success)
        {
            return new DtoCompletarPasswordFlowResult
            {
                ClearActivationCookie = true,
                Result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                    sessionResult.ErrorCode,
                    CompletarPasswordOriginMethod,
                    sessionResult.Message,
                    sessionResult.HttpCode,
                    default!)
            };
        }

        var session = sessionResult.Data;
        if (session == null)
        {
            return new DtoCompletarPasswordFlowResult
            {
                ClearActivationCookie = true,
                Result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "ACT_SES_06",
                    CompletarPasswordOriginMethod,
                    "Sesion temporal invalida.",
                    401,
                    default!)
            };
        }

        if (string.Equals(session.Purpose, NuevaPersonaSessionPurpose, StringComparison.Ordinal))
        {
            return await CompletarNuevaPersonaFlowAsync(session, request);
        }

        return await CompletarPersonaExistenteFlowAsync(session, request);
    }

    private async Task<DtoCompletarPasswordFlowResult> CompletarNuevaPersonaFlowAsync(
        DtoValidatedSession session,
        DtoCompletarPasswordInicialRequest request)
    {
        var flowId = session.FlowId;
        if (string.IsNullOrWhiteSpace(flowId))
        {
            return new DtoCompletarPasswordFlowResult
            {
                ClearActivationCookie = true,
                Result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "ACT_SES_NUP_01",
                    CompletarPasswordOriginMethod,
                    "Sesion temporal sin FlowId valido.",
                    401,
                    default!)
            };
        }

        var pending = await _registroFlowService.GetPendingPersonaAsync(flowId);
        if (pending == null)
        {
            return new DtoCompletarPasswordFlowResult
            {
                ClearActivationCookie = true,
                Result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "NUP_COMP_01",
                    CompletarPasswordOriginMethod,
                    "El registro pendiente expiró o ya fue completado. Por favor, iniciá el proceso de registro nuevamente.",
                    401,
                    default!)
            };
        }

        var crearResult = await _registroFlowService.CompletarNuevaPersona(pending, request.PasswordNueva);
        if (!crearResult.Success)
        {
            return new DtoCompletarPasswordFlowResult
            {
                ClearActivationCookie = false,
                Result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                    crearResult.ErrorCode,
                    CompletarPasswordOriginMethod,
                    crearResult.Message,
                    crearResult.HttpCode,
                    default!)
            };
        }

        var tokenResult = await GenerarTokensParaPersonaAsync(
            crearResult.Data,
            "Contraseña creada correctamente. Los tokens han sido establecidos como cookies seguras.");
        var completo = tokenResult.Success && tokenResult.Data != null;
        if (completo)
        {
            await _registroFlowService.DeletePendingPersonaAsync(flowId);
            await _registroFlowService.EliminarFlowSessionAsync(flowId);
        }

        return new DtoCompletarPasswordFlowResult
        {
            ClearActivationCookie = completo,
            Result = tokenResult
        };
    }

    private async Task<DtoCompletarPasswordFlowResult> CompletarPersonaExistenteFlowAsync(
        DtoValidatedSession session,
        DtoCompletarPasswordInicialRequest request)
    {
        if (!session.CodigoPersona.HasValue)
        {
            return new DtoCompletarPasswordFlowResult
            {
                ClearActivationCookie = true,
                Result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "ACT_SES_03",
                    CompletarPasswordOriginMethod,
                    "Sesion temporal sin persona valida.",
                    401,
                    default!)
            };
        }

        var result = await CompletarPasswordPersonaExistenteAsync(session.CodigoPersona.Value, request);

        return new DtoCompletarPasswordFlowResult
        {
            ClearActivationCookie = result.Success && result.Data != null,
            Result = result
        };
    }

    private async Task<DtoRegistroDocumentoImagenesTemporales?> ObtenerImagenesTemporalesAsync(Persona persona)
    {
        return await DocumentoIdentidadPersonaService.ObtenerImagenesTemporalesSeguroAsync(
            _documentoImagenCacheService,
            persona.TipoDocumento,
            persona.Documento,
            _logger);
    }

    private async Task EliminarImagenesTemporalesAsync(
        Persona persona,
        DtoRegistroDocumentoImagenesTemporales? imagenes)
    {
        if (imagenes is null)
        {
            return;
        }

        await DocumentoIdentidadPersonaService.EliminarImagenesTemporalesSeguroAsync(
            _documentoImagenCacheService,
            persona.TipoDocumento,
            persona.Documento,
            _logger);
    }

    private static bool CoincidePersonaRecupero(
    BusinessLogic.Entities.Persona persona,
    string tipoDocumento,
    string documento,
    string? primerApellido)
    {
        if (string.IsNullOrWhiteSpace(primerApellido))
        {
            return false;
        }

        var apellidoEntrada = DocumentUtils.NormalizarMayusculas(primerApellido);
        var apellidoPersona = !string.IsNullOrWhiteSpace(persona.PrimerApellidoMay)
            ? DocumentUtils.Normalizar(persona.PrimerApellidoMay)
            : DocumentUtils.NormalizarMayusculas(persona.PrimerApellido);

        return DocumentUtils.Normalizar(persona.TipoDocumento) == tipoDocumento
            && DocumentUtils.Normalizar(persona.Documento) == documento
            && apellidoPersona == apellidoEntrada;
    }


    private void GuardarImagenesDocumentoReconocido(
        BusinessLogic.IDevartRepositories.IUnitOfWork uow,
        Persona persona,
        DtoRegistroDocumentoImagenesTemporales? imagenes)
    {
        if (imagenes is null || _dbConnectionContext is null)
        {
            return;
        }

        DocumentoIdentidadPersonaService.GuardarImagenesDocumentoReconocido(
            uow,
            _dbConnectionContext,
            persona,
            imagenes);
    }

    private static string ObtenerCodigoValidacionDocumentoLogin(DocumentUtils.DocumentValidationError error)
    {
        return DocumentoIdentidadPersonaService.ResolverCodigoValidacionDocumento(
            error,
            "LOGIN_LDAP_02",
            "LOGIN_LDAP_03");
    }
    private static string ObtenerCodigoValidacionDocumentoRecuperarPassword(DocumentUtils.DocumentValidationError error)
    {
        return DocumentoIdentidadPersonaService.ResolverCodigoValidacionDocumento(
            error,
            "REC_PAS_02",
            "REC_PAS_03");
    }

    private static double ObtenerDiasExpiracionRefreshToken()
    {
        return JwtConfigurationHelper.GetRequiredDouble("JWT_REFRESH_EXPIRE_ADMISIONES");
    }

    private async Task<DtoAuthenticationResponse> GenerarYPersistirTokensAsync(
        Persona persona,
        long codigoPersona,
        IRefreshTokenService refreshTokenService,
        string? message = null)
    {
        var accessToken = _tokenService.GenerateAccessToken(persona);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(refreshToken);
        var refreshExpireDays = ObtenerDiasExpiracionRefreshToken();

        await refreshTokenService.SaveRefreshTokenAsync(
            codigoPersona,
            SISTEMA,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(refreshExpireDays));

        return AuthenticationResponseBuilder.Build(
            AuthenticationResponseBuilder.BuildPersonaAuth(persona),
            accessToken,
            refreshToken,
            refreshTokenHash,
            message);
    }

    public async Task<OperationResult<DtoAuthenticationResponse>> GenerarTokensParaPersonaAsync(long codigoPersona, string? message = null)
    {
        if (_serviceScopeFactory == null)
        {
            return await GenerarTokensParaPersonaCoreAsync(
                codigoPersona,
                _admisionesUowFactory,
                _refreshTokenService,
                message);
        }

        using var scope = _serviceScopeFactory.CreateScope();
        return await GenerarTokensParaPersonaCoreAsync(
            codigoPersona,
            scope.ServiceProvider.GetRequiredService<BusinessLogic.IDevartRepositories.IUnitOfWorkFactory>(),
            scope.ServiceProvider.GetRequiredService<IRefreshTokenService>(),
            message);
    }

    private async Task<OperationResult<DtoAuthenticationResponse>> GenerarTokensParaPersonaCoreAsync(
        long codigoPersona,
        BusinessLogic.IDevartRepositories.IUnitOfWorkFactory uowFactory,
        IRefreshTokenService refreshTokenService,
        string? message)
    {
        try
        {
            using var uow = uowFactory.Create();
            var persona = uow.Personas.GetByKey(codigoPersona);

            if (persona == null)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "GEN_TOK_01",
                    nameof(GenerarTokensParaPersonaAsync),
                    "Usuario no encontrado en la base de datos.",
                    404,
                    default!);
            }

            var authResponse = await GenerarYPersistirTokensAsync(
                persona,
                codigoPersona,
                refreshTokenService,
                message);

            return OperationResult<DtoAuthenticationResponse>.Ok(
                authResponse,
                nameof(GenerarTokensParaPersonaAsync));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ErrorInesperadoLog, nameof(GenerarTokensParaPersonaAsync));
            return OperationResult<DtoAuthenticationResponse>.IsFailed(
                "GEN_TOK_99",
                nameof(GenerarTokensParaPersonaAsync),
                "Error al generar tokens.",
                500,
                default!);
        }
    }
}
