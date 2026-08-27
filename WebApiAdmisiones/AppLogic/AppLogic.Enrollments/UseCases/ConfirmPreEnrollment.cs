using AppLogic.Enrollments.Constants;
using AppLogic.Contracts;
using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Mapping;
using AppLogic.Enrollments.Rules;
using AppLogic.Enrollments.Survey.Rules;
using AppLogic.Identity.Services;
using AppLogic.Integrations.EnrollmentsAndPayments.Interfaces;
using AppLogic.People.Constants;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Enrollments.UseCases;

/// <summary>
/// Confirma la preinscripcion de una o varias ofertas (nivel 1 y 2 manda una unica oferta en la
/// lista, nivel 3 y 4 puede mandar varias). Siempre llama a la variante multiple de LogicaORT,
/// que soporta ambos casos en una unica transaccion.
/// </summary>
public class ConfirmPreEnrollment(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext,
    IEnrollmentsAndPaymentsApiClient apiClient,
    ConfirmCorporatePreEnrollment confirmCorporate) : IConfirmPreEnrollment
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;
    private readonly IEnrollmentsAndPaymentsApiClient _apiClient = apiClient;
    private readonly ConfirmCorporatePreEnrollment _confirmCorporate = confirmCorporate;

    public async Task<OperationResult<ConfirmPreEnrollmentResponse>> ExecuteAsync(long personId, ConfirmPreEnrollmentRequest request)
    {
        const string methodName = nameof(ConfirmPreEnrollment);

        var rechazoRequest = PreEnrollmentConfirmationRules.ValidateRequest(request);
        if (rechazoRequest != PreEnrollmentRejection.None)
            return rechazoRequest.ToFailure<ConfirmPreEnrollmentResponse>(methodName);

        using var uow = _uowFactory.Create();

        var person = uow.Personas.GetByKey(personId);
        if (person == null)
        {
            return OperationResult<ConfirmPreEnrollmentResponse>.IsFailed("INS_CPI_05", methodName, PersonConstants.PersonaNoEncontradaMessage, 404);
        }

        var compatibleOfferings = SelectedOfferingsCompatibility.Resolve(uow, personId, request, methodName);
        if (!compatibleOfferings.Success)
            return compatibleOfferings.Failure().As<ConfirmPreEnrollmentResponse>(methodName);
        var selectedOfferings = compatibleOfferings.Data!;
        var contexto = selectedOfferings[0];

        var documentsValidation = IdentityDocumentService.ValidateIdentityDocumentsForConfirmation(uow, person, methodName);
        if (!documentsValidation.Success)
        {
            return documentsValidation.Failure().As<ConfirmPreEnrollmentResponse>(methodName);
        }

        var acceptance = PreEnrollmentConfirmationRules.EnsureStudentRegulationsAcceptance(
            uow,
            _dbConnectionContext,
            personId,
            contexto.IdProducto,
            contexto.IdComienzo,
            request.AcceptedRegulations,
            methodName);
        if (!acceptance.Success)
        {
            return acceptance.Failure().As<ConfirmPreEnrollmentResponse>(methodName);
        }

        SealSurvey(uow, personId);

        if (request.IsCorporateEnrollment)
            return _confirmCorporate.Execute(uow, personId, person, selectedOfferings, methodName);

        return await ConfirmOnlineAsync(uow, contexto, selectedOfferings, request, methodName);
    }

    private static void SealSurvey(IUnitOfWork uow, long personId)
    {
        var survey = uow.EncuestaIniAdmisions.GetByPersona(personId);
        if (survey == null
            || !string.Equals(survey.EstadoEncuestaIniAdmision, InitialSurveyState.EstadoConfirmado, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        survey.EstadoEncuestaIniAdmision = InitialSurveyState.EstadoDefinitivo;

        uow.BeginTransaction();
        try
        {
            uow.EncuestaIniAdmisions.Update(survey);
            uow.Commit();
        }
        catch
        {
            uow.Rollback();
            throw;
        }
    }

    private async Task<OperationResult<ConfirmPreEnrollmentResponse>> ConfirmOnlineAsync(
        IUnitOfWork uow,
        OfferingConfirmationData contexto,
        List<OfferingConfirmationData> selectedOfferings,
        ConfirmPreEnrollmentRequest request,
        string methodName)
    {
        var apiRequest = PreEnrollmentConfirmationRules.CreateMultipleApiRequest(contexto, request.SelectedOfferingIds);
        var apiResult = await _apiClient.ConfirmMultiplePreEnrollmentAsync(apiRequest);

        // La descripción de oferta solo existe para nivel 3 y 4 (vista de ofertas disponibles); en nivel 1 y 2 el diccionario queda vacío.
        var offeringDescriptions = uow.VdOfertasDisponibles3y4s.GetOfertasDisponibles(contexto.IdProducto)
            .GroupBy(o => o.IdOferta)
            .ToDictionary(g => g.Key, g => g.First().DescripcionOferta);
        return EnrollmentMapper.MapMultipleApiResult(apiResult, selectedOfferings, offeringDescriptions, methodName);
    }
}
