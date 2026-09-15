using AppLogic.Authentication.Contracts;
using AppLogic.Authentication.Interfaces;
using AppLogic.Authentication.UseCases;
using AppLogic.Authentication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AppLogic.Authentication.DependencyInjection;

public static class AuthenticationModule
{
    /// <summary>
    /// No registra <c>IPendingRegistrationCompletion</c>: la interfaz es de este módulo pero la
    /// implementa Registration, así que el binding vive en el composition root.
    /// </summary>
    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services)
    {
        services.AddScoped<SessionTokenIssuer>();

        services.AddScoped<IAuthenticateWithLdap, AuthenticateWithLdap>();
        services.AddScoped<IIssueTokensForPerson, IssueTokensForPerson>();
        services.AddScoped<IRefreshTokens, RefreshTokens>();
        services.AddScoped<IRecoverPassword, RecoverPassword>();
        services.AddScoped<ICompletePasswordFlow, CompletePasswordFlow>();

        services.AddScoped<IPasswordActivationService, PasswordActivationService>();
        services.AddScoped<IHashTokenStore, RedisHashTokenStore>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITwoFactorSessionStore, RedisTwoFactorSessionStore>();
        services.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>();
        services.AddScoped<ILoginFlowService, LoginFlowService>();
        return services;
    }
}
