using AppLogic.Authentication.Contracts;
using AppLogic.Authentication.Dtos;
using AppLogic.Authentication.Services;
using BusinessLogic.IDevartRepositories;
using BusinessLogic.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.Authentication.UseCases;

/// <summary>
/// Cuando hay <see cref="IServiceScopeFactory"/> resuelve la unidad de trabajo y el servicio de
/// refresh tokens en un scope propio: la emisión puede dispararse desde un flujo que ya cerró el
/// scope de la request (2FA, alta de persona nueva).
/// </summary>
public class IssueTokensForPerson(
    IUnitOfWorkFactory uowFactory,
    IRefreshTokenService refreshTokenService,
    SessionTokenIssuer tokenIssuer,
    ILogger<IssueTokensForPerson> logger,
    IServiceScopeFactory? serviceScopeFactory = null) : IIssueTokensForPerson
{
    private const string MethodName = nameof(IssueTokensForPerson);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly SessionTokenIssuer _tokenIssuer = tokenIssuer;
    private readonly ILogger<IssueTokensForPerson> _logger = logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory = serviceScopeFactory;

    public async Task<OperationResult<AuthenticationResponse>> ExecuteAsync(long personId, string? message = null)
    {
        if (_serviceScopeFactory == null)
        {
            return await IssueAsync(personId, _uowFactory, _refreshTokenService, message);
        }

        using var scope = _serviceScopeFactory.CreateScope();
        return await IssueAsync(
            personId,
            scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>(),
            scope.ServiceProvider.GetRequiredService<IRefreshTokenService>(),
            message);
    }

    private async Task<OperationResult<AuthenticationResponse>> IssueAsync(
        long personId,
        IUnitOfWorkFactory uowFactory,
        IRefreshTokenService refreshTokenService,
        string? message)
    {
        try
        {
            using var uow = uowFactory.Create();
            var person = uow.Personas.GetByKey(personId);

            if (person == null)
            {
                return OperationResult<AuthenticationResponse>.IsFailed(
                    "GEN_TOK_01",
                    MethodName,
                    "Usuario no encontrado en la base de datos.",
                    404,
                    default!);
            }

            var authResponse = await _tokenIssuer.IssueAsync(person, personId, refreshTokenService, message);

            return OperationResult<AuthenticationResponse>.Ok(authResponse, MethodName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AuthenticationLogs.UnexpectedError, MethodName);
            return OperationResult<AuthenticationResponse>.IsFailed(
                "GEN_TOK_99",
                MethodName,
                "Error al generar tokens.",
                500,
                default!);
        }
    }
}
