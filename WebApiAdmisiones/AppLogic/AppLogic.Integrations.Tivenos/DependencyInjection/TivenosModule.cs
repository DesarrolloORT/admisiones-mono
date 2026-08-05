using AppLogic.Integrations.Tivenos.Interfaces;
using AppLogic.Integrations.Tivenos.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AppLogic.Integrations.Tivenos.DependencyInjection;

public static class TivenosModule
{
    public static IServiceCollection AddTivenosIntegration(this IServiceCollection services)
    {
        services.AddScoped<ITivenosQueueService, TivenosQueueService>();
        return services;
    }
}
