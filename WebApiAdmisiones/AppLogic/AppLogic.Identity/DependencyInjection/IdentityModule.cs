using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AppLogic.Identity.DependencyInjection;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IPendingPersonStore, RedisPendingPersonStore>();
        services.AddScoped<IIdentityDocumentImageCache, RedisIdentityDocumentImageCache>();
        return services;
    }
}
