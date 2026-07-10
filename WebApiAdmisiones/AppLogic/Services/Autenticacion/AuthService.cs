using AppLogic.Dtos.Autenticacion;
using AppLogic.Dtos.Registro;
using AppLogic.Helpers.ValidationHelpers;
using AppLogic.IServices.Autenticacion;
using AppLogic.IServices.Registro;
using AppLogic.Personas.Services;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IServices;
using ConnectionContext;
using LdapService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Utilities;

namespace AppLogic.Services.Autenticacion;

public class AuthService : IAuthService
{
    private const string MensajeGenericoRecupero =
        "Si los datos ingresados son correctos, recibiras un mail con instrucciones para recuperar tu contraseña.";
    private const string SISTEMA = "ADMISIONESWEB";
    private const string ErrorInesperadoLog = "Error inesperado en {Metodo}";

    private readonly ILdap _ldap;
    private readonly BusinessLogic.IDevartRepositories.IUnitOfWorkFactory _admisionesUowFactory;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPasswordActivationService _passwordActivationService;
    private readonly IHashTokenStore _hashTokenStore;
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
    public async Task<OperationResult<DtoAuthenticationResponse>> AutenticarUsuarioLDAPAsync(string tipoDocumento, string documento, string pass)
    {
        try
        {
            // Validar el tipo de documento y documento
            var validacion = DocumentUtils.ValidarDocumentoBase(tipoDocumento, documento);
            if (!validacion.IsValid)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
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
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
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
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    authResult.ErrorCode,
                    nameof(AutenticarUsuarioLDAPAsync),
                    authResult.Message,
                    authResult.HttpCode,
                    default!);
            }

            // Generar tokens de autenticación
            var accessToken = _tokenService.GenerateAccessToken(persona);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashToken(refreshToken);
            var refreshExpireDays = ObtenerDiasExpiracionRefreshToken();

            // Guardar el refresh token en la base de datos (revoca automáticamente los anteriores)
            await _refreshTokenService.SaveRefreshTokenAsync(
                persona.CodigoPersona,
                SISTEMA,
                refreshTokenHash,
                DateTime.UtcNow.AddDays(refreshExpireDays));

            // Crear respuesta de autenticación
            var authResponse = new DtoAuthenticationResponse
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = persona.CodigoPersona,
                    PrimerNombre = persona.PrimerNombre,
                    SegundoNombre = persona.SegundoNombre,
                    PrimerApellido = persona.PrimerApellido,
                    SegundoApellido = persona.SegundoApellido,
                    TipoPersona = persona.TipoPersona,
                    Documento = persona.Documento,
                    Email = persona.Email
                },
                // Estas propiedades son internas y se usan en el controlador para establecer las cookies
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenHash = refreshTokenHash
            };

            return OperationResult<DtoAuthenticationResponse>.Ok(authResponse, nameof(AutenticarUsuarioLDAPAsync));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ErrorInesperadoLog, nameof(AutenticarUsuarioLDAPAsync));
            return OperationResult<DtoAuthenticationResponse>.IsFailed(
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

            // 4. Generar nuevos tokens
            var newAccessToken = _tokenService.GenerateAccessToken(persona);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _tokenService.HashToken(newRefreshToken);
            var refreshExpireDays = ObtenerDiasExpiracionRefreshToken();

            // 5. Guardar nuevo refresh token en la base de datos
            await _refreshTokenService.SaveRefreshTokenAsync(
                codigoPersona.Value,
                SISTEMA,
                newRefreshTokenHash,
                DateTime.UtcNow.AddDays(refreshExpireDays));

            // 6. Crear respuesta con los nuevos tokens
            var authResponse = new DtoAuthenticationResponse
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = persona.CodigoPersona,
                    PrimerNombre = persona.PrimerNombre,
                    SegundoNombre = persona.SegundoNombre,
                    PrimerApellido = persona.PrimerApellido,
                    SegundoApellido = persona.SegundoApellido,
                    TipoPersona = persona.TipoPersona,
                    Documento = persona.Documento
                },
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                RefreshTokenHash = newRefreshTokenHash,
                Message = "Tokens renovados correctamente."
            };

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
        catch (Exception)
        {
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
    public async Task<OperationResult<DtoAuthenticationResponse>> CompletarPasswordAsync(
        long codigoPersona,
        DtoCompletarPasswordInicialRequest request)
    {
        try
        {
            if (request == null)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_01",
                    nameof(CompletarPasswordAsync),
                    "La solicitud es obligatoria.",
                    400,
                    default!);
            }

            var validacionPassword = Util.ValidarPasswordNueva(request.PasswordNueva);
            if (!string.IsNullOrWhiteSpace(validacionPassword))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_02",
                    nameof(CompletarPasswordAsync),
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
                    nameof(CompletarPasswordAsync),
                    "Usuario no encontrado en la base de datos.",
                    404,
                    default!);
            }

            var storedHash = await _hashTokenStore.GetAsync(codigoPersona.ToString(CultureInfo.InvariantCulture));
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_04",
                    nameof(CompletarPasswordAsync),
                    "El link de activación ya fue utilizado o no está vigente.",
                    401,
                    default!);
            }

            var imagenes = await ObtenerImagenesTemporalesAsync(persona);
            var imagenesValidation = ValidarImagenesDocumentoReconocido(imagenes);
            if (!imagenesValidation.Success)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    imagenesValidation.ErrorCode,
                    nameof(CompletarPasswordAsync),
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
                    nameof(CompletarPasswordAsync),
                    cambioPassword.Message,
                    cambioPassword.HttpCode,
                    default!);
            }

            await _hashTokenStore.DeleteAsync(codigoPersona.ToString(CultureInfo.InvariantCulture));
            persona.FechaUltModifPassword = DateTime.Today;
            persona.UsuarioUltModifPassword = "ADMISIONES";
            GuardarImagenesDocumentoReconocido(uow, persona, imagenes);
            uow.Personas.Update(persona);
            uow.Save();
            await EliminarImagenesTemporalesAsync(persona, imagenes);

            var accessToken = _tokenService.GenerateAccessToken(persona);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashToken(refreshToken);
            var refreshExpireDays = ObtenerDiasExpiracionRefreshToken();

            await _refreshTokenService.SaveRefreshTokenAsync(
                codigoPersona,
                SISTEMA,
                refreshTokenHash,
                DateTime.UtcNow.AddDays(refreshExpireDays));

            var authResponse = new DtoAuthenticationResponse
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = persona.CodigoPersona,
                    PrimerNombre = persona.PrimerNombre,
                    SegundoNombre = persona.SegundoNombre,
                    PrimerApellido = persona.PrimerApellido,
                    SegundoApellido = persona.SegundoApellido,
                    TipoPersona = persona.TipoPersona,
                    Documento = persona.Documento,
                    Email = persona.Email
                },
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenHash = refreshTokenHash,
                Message = "Contraseña creada correctamente. Los tokens han sido establecidos como cookies seguras."
            };

            return OperationResult<DtoAuthenticationResponse>.Ok(
                authResponse,
                nameof(CompletarPasswordAsync));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ErrorInesperadoLog, nameof(CompletarPasswordAsync));
            return OperationResult<DtoAuthenticationResponse>.IsFailed(
                "INI_PAS_99",
                nameof(CompletarPasswordAsync),
                "Error al completar password inicial.",
                500,
                default!);
        }
    }

    private async Task<DtoRegistroDocumentoImagenesTemporales?> ObtenerImagenesTemporalesAsync(Persona persona)
    {
        if (_documentoImagenCacheService is null ||
            string.IsNullOrWhiteSpace(persona.TipoDocumento) ||
            string.IsNullOrWhiteSpace(persona.Documento))
        {
            return null;
        }

        try
        {
            return await _documentoImagenCacheService.ObtenerAsync(
                persona.TipoDocumento,
                persona.Documento);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "No se pudieron obtener imagenes temporales de documento para {TipoDocumento}:{Documento}.",
                persona.TipoDocumento,
                persona.Documento);
            return null;
        }
    }

    private async Task EliminarImagenesTemporalesAsync(
        Persona persona,
        DtoRegistroDocumentoImagenesTemporales? imagenes)
    {
        if (_documentoImagenCacheService is null ||
            imagenes is null ||
            string.IsNullOrWhiteSpace(persona.TipoDocumento) ||
            string.IsNullOrWhiteSpace(persona.Documento))
        {
            return;
        }

        try
        {
            await _documentoImagenCacheService.EliminarAsync(
                persona.TipoDocumento,
                persona.Documento);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "No se pudieron eliminar imagenes temporales de documento para {TipoDocumento}:{Documento}.",
                persona.TipoDocumento,
                persona.Documento);
        }
    }

    private static OperationResult<bool> ValidarImagenesDocumentoReconocido(
        DtoRegistroDocumentoImagenesTemporales? imagenes)
    {
        return DocumentoIdentidadPersonaService.ValidarImagenesDocumentoReconocido(
            imagenes,
            nameof(CompletarPasswordAsync));
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
        return error == DocumentUtils.DocumentValidationError.InvalidDocumentType
            ? "LOGIN_LDAP_02"
            : "LOGIN_LDAP_03";
    }
    private static string ObtenerCodigoValidacionDocumentoRecuperarPassword(DocumentUtils.DocumentValidationError error)
    {
        return error == DocumentUtils.DocumentValidationError.InvalidDocumentType
            ? "REC_PAS_02"
            : "REC_PAS_03";
    }

    private static double ObtenerDiasExpiracionRefreshToken()
    {
        var rawValue = Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRE_ADMISIONES");
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException("La variable de entorno JWT_REFRESH_EXPIRE_ADMISIONES no está configurada.");
        }

        if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var days))
        {
            throw new InvalidOperationException("La variable de entorno JWT_REFRESH_EXPIRE_ADMISIONES tiene un valor inválido.");
        }

        return days;
    }

    public async Task<OperationResult<DtoAuthenticationResponse>> GenerarTokensParaPersonaAsync(long codigoPersona)
    {
        if (_serviceScopeFactory == null)
        {
            return await GenerarTokensParaPersonaCoreAsync(
                codigoPersona,
                _admisionesUowFactory,
                _refreshTokenService);
        }

        using var scope = _serviceScopeFactory.CreateScope();
        return await GenerarTokensParaPersonaCoreAsync(
            codigoPersona,
            scope.ServiceProvider.GetRequiredService<BusinessLogic.IDevartRepositories.IUnitOfWorkFactory>(),
            scope.ServiceProvider.GetRequiredService<IRefreshTokenService>());
    }

    private async Task<OperationResult<DtoAuthenticationResponse>> GenerarTokensParaPersonaCoreAsync(
        long codigoPersona,
        BusinessLogic.IDevartRepositories.IUnitOfWorkFactory uowFactory,
        IRefreshTokenService refreshTokenService)
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

            var accessToken = _tokenService.GenerateAccessToken(persona);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashToken(refreshToken);
            var refreshExpireDays = ObtenerDiasExpiracionRefreshToken();

            await refreshTokenService.SaveRefreshTokenAsync(
                codigoPersona,
                SISTEMA,
                refreshTokenHash,
                DateTime.UtcNow.AddDays(refreshExpireDays));

            return OperationResult<DtoAuthenticationResponse>.Ok(
                new DtoAuthenticationResponse
                {
                    Persona = new DtoPersonaAuth
                    {
                        CodigoPersona = persona.CodigoPersona,
                        PrimerNombre = persona.PrimerNombre,
                        SegundoNombre = persona.SegundoNombre,
                        PrimerApellido = persona.PrimerApellido,
                        SegundoApellido = persona.SegundoApellido,
                        TipoPersona = persona.TipoPersona,
                        Documento = persona.Documento,
                        Email = persona.Email
                    },
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RefreshTokenHash = refreshTokenHash,
                    Message = "Contraseña creada correctamente. Los tokens han sido establecidos como cookies seguras."
                },
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
