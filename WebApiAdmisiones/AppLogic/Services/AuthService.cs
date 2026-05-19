using AppLogic.DTOs;
using AppLogic.Helpers;
using AppLogic.IServices;
using AppLogic.Utilities;
using BusinessLogic.IServices;
using LdapService.Interfaces;
using System.Globalization;
using Utilities;

namespace AppLogic.Services;

public class AuthService : IAuthService
{
    private readonly ILdap _ldap;
    private readonly BusinessLogic.IDevartRepositories.IUnitOfWorkFactory _admisionesUowFactory;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;

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
        IRefreshTokenService refreshTokenService)
    {
        _ldap = ldap;
        _admisionesUowFactory = admisionesUowFactory;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }

    /// <summary>
    /// Autentica un usuario contra el servicio LDAP delegando al proyecto Autenticacion
    /// y, si es exitoso, obtiene la Persona desde la base de datos y genera los tokens de autenticación.
    /// </summary>
    /// <param name="codigoPersona">Código de la persona a autenticar.</param>
    /// <param name="pass">Contraseña del usuario.</param>
    /// <returns>OperationResult con la respuesta de autenticación incluyendo tokens y la Persona autenticada si el login es exitoso.</returns>
    public async Task<OperationResult<DtoAuthenticationResponse>> AutenticarUsuarioLDAPAsync(long codigoPersona, string pass)
    {
        try
        {
            // Delegar la autenticación LDAP al servicio de Core/Autenticacion
            var authResult = await _ldap.AutenticarUsuarioLDAPAsync(codigoPersona, pass);

            if (!authResult.Success)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    authResult.ErrorCode,
                    nameof(AutenticarUsuarioLDAPAsync),
                    authResult.Message,
                    authResult.HttpCode,
                    default!);
            }

            // Login exitoso: obtener la Persona desde la base de datos
            using var admisionesUow = _admisionesUowFactory.Create();
            var persona = admisionesUow.Personas.GetByKey(codigoPersona);

            if (persona == null)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "LOGIN_LDAP_04",
                    nameof(AutenticarUsuarioLDAPAsync),
                    "Usuario autenticado pero no se encontró la persona en la base de datos.",
                    404,
                    default!);
            }

            // Generar tokens de autenticación
            var accessToken = _tokenService.GenerateAccessToken(persona);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashToken(refreshToken);
            var refreshExpireDays = ObtenerDiasExpiracionRefreshToken();

            // Guardar el refresh token en la base de datos (revoca automáticamente los anteriores)
            await _refreshTokenService.SaveRefreshTokenAsync(
                codigoPersona,
                "ADMISIONESWEB",
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
                    Documento = persona.Documento
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
            return OperationResult<DtoAuthenticationResponse>.IsFailed(
                "LOGIN_LDAP_99",
                nameof(AutenticarUsuarioLDAPAsync),
                $"Error al autenticar usuario: {ex.Message}",
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
                "ADMISIONESWEB", refreshTokenHash);

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
                "ADMISIONESWEB",
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
            return OperationResult<DtoAuthenticationResponse>.IsFailed(
                "REFRESH_TOKEN_99",
                nameof(RefrescarTokensAsync),
                $"Error al refrescar tokens: {ex.Message}",
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

            using var uow = _admisionesUowFactory.Create();
            var tipoDocumento = DocumentUtils.Normalizar(request.TipoDocumento);
            var documento = DocumentUtils.Normalizar(request.Documento);
            var persona = uow.Personas.GetByDocumento(documento);

            if (persona == null)
            {
                return OperationResult<object>.IsFailed(
                    "REC_PAS_04",
                    nameof(RecuperarPassword),
                    $"No se encontró persona con el numero de documento {documento}",
                    404);
            }

            var envioContrasenia = await _ldap.EnviarContrasenia(
                string.Empty,
                tipoDocumento,
                documento,
                request.PrimerApellido!.Trim(),
                "ADMISIONES",
                "SOLICITUD_DE_CONTRASEÑA");

            if (!envioContrasenia.Success)
            {
                return OperationResult<object>.IsFailed(
                    "REC_PAS_05",
                    nameof(RecuperarPassword),
                    envioContrasenia.Message,
                    envioContrasenia.HttpCode);
            }

            return OperationResult<object>.Ok(
                envioContrasenia.Data ?? string.Empty,
                nameof(RecuperarPassword));
        }
        catch (Exception ex)
        {
            return OperationResult<object>.IsFailed(
               "REC_PAS_99",
               nameof(RecuperarPassword),
               $"Error al recuperar contraseña: {ex.Message}",
               500,
               default!);
        }
    }

    /// <summary>
    /// Cambia la password del usuario autenticado en LDAP.
    /// </summary>
    /// <param name="codigoPersona">Codigo de persona del usuario autenticado.</param>
    /// <param name="request">Passwords actual y nueva.</param>
    /// <returns>Resultado del cambio de password.</returns>
    public async Task<OperationResult<object>> CambiarPasswordAsync(long codigoPersona, DtoCambiarPasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return OperationResult<object>.IsFailed(
                    "CAM_PAS_01",
                    nameof(CambiarPasswordAsync),
                    "La solicitud es obligatoria.",
                    400);
            }

            var validacionPassword = Util.ValidarPassword(request.PasswordActual, request.PasswordNueva);
            if (!string.IsNullOrWhiteSpace(validacionPassword))
            {
                return OperationResult<object>.IsFailed(
                    "CAM_PAS_02",
                    nameof(CambiarPasswordAsync),
                    validacionPassword,
                    400);
            }

            var cambioPassword = await _ldap.CambiarPasswordAsync(
                codigoPersona.ToString(CultureInfo.InvariantCulture),
                request.PasswordActual,
                request.PasswordNueva);

            if (!cambioPassword.Success)
            {
                return OperationResult<object>.IsFailed(
                    cambioPassword.ErrorCode,
                    nameof(CambiarPasswordAsync),
                    cambioPassword.Message,
                    cambioPassword.HttpCode);
            }

            return OperationResult<object>.Ok(
                "Se actualizó tu contraseña",
                nameof(CambiarPasswordAsync));
        }
        catch (Exception ex)
        {
            return OperationResult<object>.IsFailed(
               "CAM_PAS_99",
               nameof(CambiarPasswordAsync),
               $"Error al cambiar contraseña: {ex.Message}",
               500,
               default!);
        }
    }

    /// <summary>
    /// Completa el alta de password inicial usando una sesion temporal de activacion.
    /// </summary>
    /// <param name="codigoPersona">Codigo de persona resuelto desde la sesion temporal.</param>
    /// <param name="request">Nueva password a establecer.</param>
    /// <returns>Respuesta de autenticacion normal con tokens para cookies.</returns>
    public async Task<OperationResult<DtoAuthenticationResponse>> CompletarPasswordInicialAsync(
        long codigoPersona,
        DtoCompletarPasswordInicialRequest request)
    {
        try
        {
            if (request == null)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_01",
                    nameof(CompletarPasswordInicialAsync),
                    "La solicitud es obligatoria.",
                    400,
                    default!);
            }

            var validacionPassword = Util.ValidarPasswordNueva(request.PasswordNueva);
            if (!string.IsNullOrWhiteSpace(validacionPassword))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_02",
                    nameof(CompletarPasswordInicialAsync),
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
                    nameof(CompletarPasswordInicialAsync),
                    "Usuario no encontrado en la base de datos.",
                    404,
                    default!);
            }

            if (string.IsNullOrWhiteSpace(persona.HashTokenPassword))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "INI_PAS_04",
                    nameof(CompletarPasswordInicialAsync),
                    "El link de activación ya fue utilizado o no está vigente.",
                    401,
                    default!);
            }

            var cambioPassword = await _ldap.ForzarCambiarPasswordAsync(
                codigoPersona.ToString(CultureInfo.InvariantCulture),
                request.PasswordNueva);

            if (!cambioPassword.Success)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    cambioPassword.ErrorCode,
                    nameof(CompletarPasswordInicialAsync),
                    cambioPassword.Message,
                    cambioPassword.HttpCode,
                    default!);
            }

            persona.HashTokenPassword = null;
            persona.FechaUltModifPassword = DateTime.Today;
            persona.UsuarioUltModifPassword = "ADMISIONES";
            uow.Personas.Update(persona);
            uow.Save();

            var accessToken = _tokenService.GenerateAccessToken(persona);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashToken(refreshToken);
            var refreshExpireDays = ObtenerDiasExpiracionRefreshToken();

            await _refreshTokenService.SaveRefreshTokenAsync(
                codigoPersona,
                "ADMISIONESWEB",
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
                    Documento = persona.Documento
                },
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenHash = refreshTokenHash,
                Message = "Contraseña creada correctamente. Los tokens han sido establecidos como cookies seguras."
            };

            return OperationResult<DtoAuthenticationResponse>.Ok(
                authResponse,
                nameof(CompletarPasswordInicialAsync));
        }
        catch (Exception ex)
        {
            return OperationResult<DtoAuthenticationResponse>.IsFailed(
                "INI_PAS_99",
                nameof(CompletarPasswordInicialAsync),
                $"Error al completar password inicial: {ex.Message}",
                500,
                default!);
        }
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
}
