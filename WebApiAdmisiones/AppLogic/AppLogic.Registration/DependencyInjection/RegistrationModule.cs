using AppLogic.Authentication.Interfaces;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Interfaces;
using AppLogic.Registration.Services;
using AppLogic.Registration.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace AppLogic.Registration.DependencyInjection;

public static class RegistrationModule
{
    public static IServiceCollection AddRegistration(this IServiceCollection services)
    {
        services.AddScoped<LdapUserDirectory>();

        services.AddScoped<IEvaluateDocument, EvaluateDocument>();
        services.AddScoped<IVerifyIdentity, VerifyIdentity>();
        services.AddScoped<IValidateNewPerson, ValidateNewPerson>();
        services.AddScoped<ICompleteNewPerson, CompleteNewPerson>();
        services.AddScoped<IConfirmRegistrationRequest, ConfirmRegistrationRequest>();

        services.AddScoped<IRegistrationFlowService, RegistrationFlowService>();

        // Authentication declara IPendingRegistrationCompletion y este módulo la implementa:
        // inversión de dependencia para que Authentication no referencie Registration.
        services.AddScoped<IPendingRegistrationCompletion>(sp =>
            sp.GetRequiredService<IRegistrationFlowService>());

        return services;
    }
}
