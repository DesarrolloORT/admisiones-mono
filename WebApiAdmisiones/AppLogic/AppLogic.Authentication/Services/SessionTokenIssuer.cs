using AppLogic.Authentication.Dtos;
using AppLogic.Authentication.Interfaces;
using AppLogic.Authentication.Rules;
using AppLogic.Authentication.Security;
using BusinessLogic.Entities;
using BusinessLogic.IServices;

namespace AppLogic.Authentication.Services;

/// <summary>
/// Emite el par access + refresh token y persiste el hash del refresh. Lo comparten los tres caminos
/// que terminan con sesión iniciada: login, refresh y alta de contraseña inicial.
/// </summary>
public class SessionTokenIssuer(ITokenService tokenService)
{
    /// <summary>Sistema con el que se guardan los refresh tokens en T_REFRESH_TOKEN.</summary>
    public const string SystemName = "ADMISIONESWEB";

    private readonly ITokenService _tokenService = tokenService;

    /// <summary>
    /// El <see cref="IRefreshTokenService"/> viene por parámetro y no inyectado porque
    /// <c>IssueTokensForPerson</c> puede resolverlo desde un scope propio.
    /// </summary>
    public async Task<AuthenticationResponse> IssueAsync(
        Persona person,
        long personId,
        IRefreshTokenService refreshTokenService,
        string? message = null)
    {
        var accessToken = _tokenService.GenerateAccessToken(person);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(refreshToken);
        var refreshExpireDays = JwtConfiguration.GetRequiredDouble("JWT_REFRESH_EXPIRE_ADMISIONES");

        await refreshTokenService.SaveRefreshTokenAsync(
            personId,
            SystemName,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(refreshExpireDays));

        return AuthenticationResponseBuilder.Build(
            AuthenticationResponseBuilder.BuildAuthenticatedPerson(person),
            accessToken,
            refreshToken,
            refreshTokenHash,
            message);
    }
}
