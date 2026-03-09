using AppLogic.DTOs;
using AppLogic.IServices;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IServices;
using LdapService.Interfaces;
using Utilities;

namespace AppLogic.Services;

/// <summary>
/// Servicio de autenticación para Admisiones.
/// Delega la verificación de credenciales al <see cref="ILdap"/> del Core,
/// genera tokens JWT y gestiona refresh tokens.
/// </summary>
public class AuthService : IAuthService
{
    private const string Sistema = "ADMISIONESWEB";
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(7);

    private readonly ILdap _ldap;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWorkFactory _uowFactory;

    public AuthService(
        ILdap ldap,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IUnitOfWorkFactory uowFactory)
    {
        _ldap = ldap;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _uowFactory = uowFactory;
    }

    /// <inheritdoc/>
    public async Task<OperationResult<DTOAuthenticationResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var authResult = await _ldap.AutenticarUsuarioLDAPAsync(request.CodigoPersona, request.Password);

            if (!authResult.Success || authResult.Data == false)
            {
                return OperationResult<DTOAuthenticationResponse>.IsFailed(
                    authResult.ErrorCode,
                    nameof(LoginAsync),
                    authResult.Message,
                    authResult.HttpCode,
                    default!);
            }

            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetByKey(request.CodigoPersona);

            if (persona == null)
            {
                return OperationResult<DTOAuthenticationResponse>.IsFailed(
                    "LOGIN_LDAP_04",
                    nameof(LoginAsync),
                    "Usuario autenticado pero no se encontró la persona en la base de datos.",
                    404,
                    default!);
            }

            var accessToken = _tokenService.GenerateAccessToken(persona);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashToken(refreshToken);

            await _refreshTokenService.SaveRefreshTokenAsync(
                persona.CodigoPersona, Sistema, refreshTokenHash,
                DateTime.UtcNow.Add(RefreshTokenTtl));

            return OperationResult<DTOAuthenticationResponse>.Ok(
                new DTOAuthenticationResponse
                {
                    Persona = new DTOPersonaAuth
                    {
                        CodigoPersona = persona.CodigoPersona,
                        PrimerNombre = persona.PrimerNombre?.Trim(),
                        SegundoNombre = persona.SegundoNombre?.Trim(),
                        PrimerApellido = persona.PrimerApellido?.Trim(),
                        SegundoApellido = persona.SegundoApellido?.Trim(),
                        TipoPersona = persona.TipoPersona,
                        Documento = persona.Documento?.Trim()
                    },
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RefreshTokenHash = refreshTokenHash
                },
                nameof(LoginAsync));
        }
        catch (Exception ex)
        {
            return OperationResult<DTOAuthenticationResponse>.IsFailed(
                "LOGIN_LDAP_99",
                nameof(LoginAsync),
                $"Error al autenticar usuario: {ex.Message}",
                500,
                default!);
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult<DTOAuthenticationResponse>> RefrescarTokensAsync(string? refreshToken, string? codigoPersonaClaim)
    {
        try
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return OperationResult<DTOAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_01", nameof(RefrescarTokensAsync),
                    "No se encontró refresh token en las cookies.", 401, default!);
            }

            if (string.IsNullOrEmpty(codigoPersonaClaim) || !long.TryParse(codigoPersonaClaim, out var codigoPersona))
            {
                return OperationResult<DTOAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_02", nameof(RefrescarTokensAsync),
                    "Token de acceso inválido o expirado.", 401, default!);
            }

            var refreshTokenHash = _tokenService.HashToken(refreshToken);
            var isValid = await _refreshTokenService.ValidateRefreshTokenAsync(codigoPersona, Sistema, refreshTokenHash);

            if (!isValid)
            {
                return OperationResult<DTOAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_03", nameof(RefrescarTokensAsync),
                    "Refresh token inválido o expirado.", 401, default!);
            }

            using var uow = _uowFactory.Create();
            var persona = uow.Personas.GetByKey(codigoPersona);

            if (persona == null)
            {
                return OperationResult<DTOAuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_04", nameof(RefrescarTokensAsync),
                    "Usuario no encontrado en la base de datos.", 404, default!);
            }

            var newAccessToken = _tokenService.GenerateAccessToken(persona);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _tokenService.HashToken(newRefreshToken);

            await _refreshTokenService.SaveRefreshTokenAsync(
                codigoPersona, Sistema, newRefreshTokenHash,
                DateTime.UtcNow.Add(RefreshTokenTtl));

            return OperationResult<DTOAuthenticationResponse>.Ok(
                new DTOAuthenticationResponse
                {
                    Persona = new DTOPersonaAuth
                    {
                        CodigoPersona = persona.CodigoPersona,
                        PrimerNombre = persona.PrimerNombre?.Trim(),
                        SegundoNombre = persona.SegundoNombre?.Trim(),
                        PrimerApellido = persona.PrimerApellido?.Trim(),
                        SegundoApellido = persona.SegundoApellido?.Trim(),
                        TipoPersona = persona.TipoPersona,
                        Documento = persona.Documento?.Trim()
                    },
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    RefreshTokenHash = newRefreshTokenHash,
                    Message = "Tokens renovados correctamente."
                },
                nameof(RefrescarTokensAsync));
        }
        catch (Exception ex)
        {
            return OperationResult<DTOAuthenticationResponse>.IsFailed(
                "REFRESH_TOKEN_99", nameof(RefrescarTokensAsync),
                $"Error al refrescar tokens: {ex.Message}", 500, default!);
        }
    }
}

