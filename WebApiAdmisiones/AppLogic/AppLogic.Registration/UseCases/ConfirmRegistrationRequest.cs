using AppLogic.Contracts.Text;
using AppLogic.Identity;
using AppLogic.Registration.Constants;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Dtos;
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

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;
    private readonly ILogger<ConfirmRegistrationRequest> _logger = logger;

    public Task<OperationResult<object?>> ExecuteAsync(RegisterPersonRequest request)
    {
        if (request == null)
        {
            return Task.FromResult(OperationResult<object?>.IsFailed(
                RegistrationErrorCodes.MissingRequest,
                MethodName,
                RegistrationErrorCodes.MissingRequestMessage,
                400));
        }

        var documentValidation = IdentityDocumentRules.ValidateBaseDocument(request.DocumentType, request.DocumentNumber);
        if (!documentValidation.IsValid)
        {
            return Task.FromResult(OperationResult<object?>.IsFailed(
                RegistrationValidation.ResolveDocumentValidationCode(documentValidation.Error),
                MethodName,
                documentValidation.Message,
                400));
        }

        var documentType = TextNormalization.Trim(request.DocumentType);
        if (IdentityDocumentRules.IsNationalId(documentType))
        {
            return Task.FromResult(OperationResult<object?>.IsFailed(
                RegistrationErrorCodes.UnsupportedDocumentType,
                MethodName,
                "ConfirmRegistrationRequest solo aplica para documentos distintos a cédula de identidad.",
                400));
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

            return Task.FromResult(OperationResult<object?>.IsSuccess(
                null,
                MethodName,
                "La solicitud de alta quedó registrada."));
        }
        catch (Exception ex)
        {
            uow.Rollback();
            _logger.LogError(ex, ErrorInesperadoLog, MethodName);
            return Task.FromResult(OperationResult<object?>.IsFailed(
                "REG_SOLICITUD_99",
                MethodName,
                "Error al crear la solicitud de alta.",
                500));
        }
    }
}
