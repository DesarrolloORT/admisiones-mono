using AppLogic.Authentication.Contracts;
using AppLogic.Authentication.Dtos;
using AppLogic.Authentication.Interfaces;
using AppLogic.Authentication.Services;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IServices;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.Authentication.UseCases;

public class RefreshTokens(
    IUnitOfWorkFactory uowFactory,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    SessionTokenIssuer tokenIssuer,
    ILogger<RefreshTokens> logger) : IRefreshTokens
{
    private const string MethodName = nameof(RefreshTokens);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly SessionTokenIssuer _tokenIssuer = tokenIssuer;
    private readonly ILogger<RefreshTokens> _logger = logger;

    public async Task<OperationResult<AuthenticationResponse>> ExecuteAsync(string? refreshToken)
    {
        try
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return OperationResult<AuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_01",
                    MethodName,
                    "No se encontró refresh token en las cookies.",
                    401,
                    default!);
            }

            var refreshTokenHash = _tokenService.HashToken(refreshToken);
            var personId = await _refreshTokenService.GetCodigoPersonaByRefreshTokenAsync(
                SessionTokenIssuer.SystemName, refreshTokenHash);

            if (!personId.HasValue)
            {
                return OperationResult<AuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_03",
                    MethodName,
                    "Refresh token inválido o expirado.",
                    401,
                    default!);
            }

            using var uow = _uowFactory.Create();
            var person = uow.Personas.GetByKey(personId.Value);

            if (person == null)
            {
                return OperationResult<AuthenticationResponse>.IsFailed(
                    "REFRESH_TOKEN_04",
                    MethodName,
                    "Usuario no encontrado en la base de datos.",
                    404,
                    default!);
            }

            var authResponse = await _tokenIssuer.IssueAsync(
                person,
                personId.Value,
                _refreshTokenService,
                "Tokens renovados correctamente.");

            return OperationResult<AuthenticationResponse>.Ok(authResponse, MethodName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AuthenticationLogs.UnexpectedError, MethodName);
            return OperationResult<AuthenticationResponse>.IsFailed(
                "REFRESH_TOKEN_99",
                MethodName,
                "Error al refrescar tokens.",
                500,
                default!);
        }
    }
}
