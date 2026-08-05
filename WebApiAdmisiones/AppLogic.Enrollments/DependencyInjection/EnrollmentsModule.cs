using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Survey.Services;
using AppLogic.Enrollments.Interfaces;
using AppLogic.Enrollments.Services;
using AppLogic.Enrollments.UseCases;
using AppLogic.Enrollments.UseCases.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace AppLogic.Enrollments.DependencyInjection;

/// <summary>
/// Registro del módulo. El host solo llama a <see cref="AddEnrollments"/>: qué clase implementa
/// cada caso de uso es asunto del módulo.
/// </summary>
public static class EnrollmentsModule
{
    public static IServiceCollection AddEnrollments(this IServiceCollection services)
    {
        // Un proceso de negocio por clase.
        services.AddScoped<IRegisterProductInterest, RegisterProductInterest>();
        services.AddScoped<ConfirmCorporatePreEnrollment>();
        services.AddScoped<IConfirmPreEnrollment, ConfirmPreEnrollment>();
        services.AddScoped<IReactivateEnrollment, ReactivateEnrollment>();
        services.AddScoped<IGetEnrollmentDetails, GetEnrollmentDetails>();
        services.AddScoped<IGetStudentRegulationsAcceptance, GetStudentRegulationsAcceptance>();

        // Pagos: el dispatcher solo rutea; cada método de pago es su propio caso de uso.
        services.AddScoped<IPayWithPersonalAccount, PayWithPersonalAccount>();
        services.AddScoped<IRegisterExternalPaymentMethod, RegisterExternalPaymentMethod>();
        services.AddScoped<IGenerateInvoicePaymentUrl, GenerateInvoicePaymentUrl>();
        services.AddScoped<IStartEnrollmentPayment, StartEnrollmentPayment>();

        services.AddScoped<IInitialSurveyService, InitialSurveyService>();
        services.AddScoped<IAdmissionDueDateCalculator, AdmissionDueDateCalculator>();

        return services;
    }
}
