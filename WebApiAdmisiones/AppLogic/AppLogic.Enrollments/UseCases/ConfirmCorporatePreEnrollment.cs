using AppLogic.Contracts;
using AppLogic.Enrollments.Constants;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Rules;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModBandejaAppLogic.Interfaces;
using Utilities;

namespace AppLogic.Enrollments.UseCases;

/// <summary>
/// Rama corporativa de la confirmación: un trámite + una instancia + una fila en
/// T_INST_WORKFLOW_INSCRIPCION por oferta. Recibe el uow del caso de uso que la invoca
/// porque comparte su transacción.
/// </summary>
public class ConfirmCorporatePreEnrollment(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IConfiguration _configuration = configuration;

    internal OperationResult<ConfirmPreEnrollmentResponse> Execute(
        IUnitOfWork uow,
        long personId,
        Persona person,
        List<OfferingConfirmationData> selectedOfferings,
        string methodName)
    {
        var contexto = selectedOfferings[0];
        if (!PreEnrollmentConfirmationRules.IsLevelEnabledForCorporate(contexto))
            return PreEnrollmentRejection.CorporateRequiresLevel3Or4.ToFailure<ConfirmPreEnrollmentResponse>(methodName);

        uow.BeginTransaction();
        try
        {
            foreach (var offering in selectedOfferings)
            {
                var xml = PreEnrollmentConfirmationRules.BuildCorporateInstanceXml(offering, person);
                var dtoTramite = PreEnrollmentConfirmationRules.CreateCorporateCase(personId);
                var dtoInstancia = PreEnrollmentConfirmationRules.CreateCorporateInstance(offering, personId, xml);
                var dtosBandeja = PreEnrollmentConfirmationRules.CreateCorporateInboxes(
                    _configuration, EnrollmentConstants.CorporateInbox.UsuarioSistema);

                // El IBandejaService del modulo Bandeja (Core) dispone su DbContext al terminar cada
                // llamada, aunque ese contexto es compartido (scoped) por DI. Con varias ofertas este
                // metodo se llama mas de una vez por request, y la segunda llamada reventaria con
                // ObjectDisposedException si reusara el service del scope principal. Se resuelve en un
                // scope de DI propio por oferta para que cada llamada tenga su propio contexto.
                using var bandejaScope = _serviceScopeFactory.CreateScope();
                var bandejaService = bandejaScope.ServiceProvider.GetRequiredService<IBandejaService>();
                var altaResult = bandejaService.AltaTramiteWorkflow(dtoTramite, dtoInstancia, dtosBandeja);

                if (!altaResult.Success)
                {
                    uow.Rollback();
                    return altaResult.Failure().As<ConfirmPreEnrollmentResponse>(methodName);
                }

                uow.InstWorkflowInscripcions.Add(
                    PreEnrollmentConfirmationRules.CreateCorporateWorkflowEnrollment(offering, altaResult.Data));
            }

            uow.Commit();
        }
        catch
        {
            uow.Rollback();
            throw;
        }

        return OperationResult<ConfirmPreEnrollmentResponse>.Ok(
            PreEnrollmentConfirmationRules.MapCorporateResult(), methodName);
    }
}
