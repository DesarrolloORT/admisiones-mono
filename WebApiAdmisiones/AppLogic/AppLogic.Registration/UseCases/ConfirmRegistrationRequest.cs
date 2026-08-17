using AppLogic.Contracts;
using AppLogic.Contracts.Text;
using AppLogic.Identity;
using AppLogic.Registration.Constants;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Interfaces;
using AppLogic.Registration.Mapping;
using AppLogic.Registration.Services;
using AppLogic.Registration.Validators;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.Registration.UseCases;

public class ConfirmRegistrationRequest(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext,
    ILogger<ConfirmRegistrationRequest> logger) : IConfirmRegistrationRequest
{
    private const string MethodName = nameof(ConfirmRegistrationRequest);
    private const string ErrorInesperadoLog = "Error inesperado en {Metodo}";
    private const string SolicitudRegistradaMessage = "La solicitud de alta quedó registrada.";

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;
    private readonly ILogger<ConfirmRegistrationRequest> _logger = logger;

    public Task<OperationResult<RegistrationFlowResult>> ExecuteAsync(RegisterPersonRequest request)
    {
        if (request == null)
        {
            return Task.FromResult(OperationResult<RegistrationFlowResult>.IsFailed(
                RegistrationErrorCodes.MissingRequest,
                MethodName,
                RegistrationErrorCodes.MissingRequestMessage,
                400));
        }

        var documentValidation = IdentityDocumentRules.ValidateBaseDocument(request.DocumentType, request.DocumentNumber);
        if (!documentValidation.IsValid)
        {
            return Task.FromResult(OperationResult<RegistrationFlowResult>.IsFailed(
                RegistrationValidation.ResolveDocumentValidationCode(documentValidation.Error),
                MethodName,
                documentValidation.Message,
                400));
        }

        var documentType = TextNormalization.Trim(request.DocumentType);
        if (IdentityDocumentRules.IsNationalId(documentType))
        {
            return Task.FromResult(OperationResult<RegistrationFlowResult>.IsFailed(
                RegistrationErrorCodes.UnsupportedDocumentType,
                MethodName,
                "ConfirmRegistrationRequest solo aplica para documentos distintos a cédula de identidad.",
                400));
        }

        // T_SOLICITUD_ALTA no tiene columna de característica de país: solo se valida el teléfono.
        var primaryPhone = RegistrationValidation.ValidatePrimaryPhone(request.PrimaryPhone, MethodName);
        if (!primaryPhone.Success)
        {
            return Task.FromResult(primaryPhone.Failure().As<RegistrationFlowResult>(MethodName));
        }

        using var uow = _uowFactory.Create();

        try
        {
            uow.BeginTransaction();
            var registrationRequest = RegistrationMapper.CreateRegistrationRequest(
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_SOLICITUD_ALTA),
                request);

            uow.SolicitudAltas.Add(registrationRequest);
            AdmissionRecords.AddForRegistrationRequest(uow, _dbConnectionContext, registrationRequest.IdSolicitudAlta);
            uow.Commit();

            // PendingReview marca el final del flujo: no hay persona, ni usuario LDAP, ni mail.
            return Task.FromResult(OperationResult<RegistrationFlowResult>.IsSuccess(
                new RegistrationFlowResult(SolicitudRegistradaMessage, MailSent: false, PendingReview: true),
                MethodName,
                SolicitudRegistradaMessage));
        }
        catch (Exception ex)
        {
            uow.Rollback();
            _logger.LogError(ex, ErrorInesperadoLog, MethodName);
            return Task.FromResult(OperationResult<RegistrationFlowResult>.IsFailed(
                "REG_SOLICITUD_99",
                MethodName,
                "Error al crear la solicitud de alta.",
                500));
        }
    }
}
