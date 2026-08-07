using AppLogic.Scholarships.Interfaces;
using AppLogic.Scholarships.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AppLogic.Scholarships.DependencyInjection;

public static class ScholarshipsModule
{
    public static IServiceCollection AddScholarships(this IServiceCollection services)
    {
        services.AddScoped<IScholarshipService, ScholarshipService>();
        services.AddScoped<IScholarshipFundService, ScholarshipFundService>();
        return services;
    }
}
