using AppLogic.DTOs;
using AppLogic.IServices;
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
    /// <param name="codigoPersonaClaim">Código de persona extraído del access token actual.</param>
    /// <returns>OperationResult con los nuevos tokens generados.</returns>
    public async Task<OperationResult<DtoAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken, string? codigoPersonaClaim)
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

            // 2. Validar y parsear el código de persona
            if (string.IsNullOrEmpty(codigoPersonaClaim) || !long.TryParse(codigoPersonaClaim, out var codigoPersona))
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_02",
                    nameof(RefrescarTokensAsync),
                    "Token de acceso inválido o expirado.",
                    401,
                    default!);
            }

            // 3. Validar refresh token en la base de datos
            var refreshTokenHash = _tokenService.HashToken(refreshToken);
            var isValid = await _refreshTokenService.ValidateRefreshTokenAsync(
                codigoPersona, "ADMISIONESWEB", refreshTokenHash);

            if (!isValid)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_03",
                    nameof(RefrescarTokensAsync),
                    "Refresh token inválido o expirado.",
                    401,
                    default!);
            }

            // 4. Obtener persona de la base de datos
            using var admisionesUow = _admisionesUowFactory.Create();
            var persona = admisionesUow.Personas.GetByKey(codigoPersona);

            if (persona == null)
            {
                return OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_04",
                    nameof(RefrescarTokensAsync),
                    "Usuario no encontrado en la base de datos.",
                    404,
                    default!);
            }

            // 5. Generar nuevos tokens
            var newAccessToken = _tokenService.GenerateAccessToken(persona);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _tokenService.HashToken(newRefreshToken);
            var refreshExpireDays = ObtenerDiasExpiracionRefreshToken();

            // 6. Guardar nuevo refresh token en la base de datos
            await _refreshTokenService.SaveRefreshTokenAsync(
                codigoPersona,
                "ADMISIONESWEB",
                newRefreshTokenHash,
                DateTime.UtcNow.AddDays(refreshExpireDays));

            // 7. Crear respuesta con los nuevos tokens
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
