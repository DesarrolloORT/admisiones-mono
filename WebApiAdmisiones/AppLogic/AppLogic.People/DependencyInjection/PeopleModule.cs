using AppLogic.People.Contracts;
using AppLogic.People.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace AppLogic.People.DependencyInjection;

public static class PeopleModule
{
    public static IServiceCollection AddPeople(this IServiceCollection services)
    {
        services.AddScoped<IGetPersonDetails, GetPersonDetails>();
        services.AddScoped<IUpdatePersonDetails, UpdatePersonDetails>();
        services.AddScoped<IValidatePhoneNumber, ValidatePhoneNumber>();
        services.AddScoped<IGetMyEnrollments, GetMyEnrollments>();
        services.AddScoped<IChangePassword, ChangePassword>();
        services.AddScoped<IGetPersonPhoto, GetPersonPhoto>();
        services.AddScoped<IUploadPersonPhoto, UploadPersonPhoto>();
        services.AddScoped<IGetPersonIdentityDocument, GetPersonIdentityDocument>();
        services.AddScoped<IUploadPersonIdentityDocument, UploadPersonIdentityDocument>();
        return services;
    }
}
