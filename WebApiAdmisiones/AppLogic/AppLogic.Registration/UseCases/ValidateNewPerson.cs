using AppLogic.Contracts.Text;
using AppLogic.Identity;
using AppLogic.Registration.Constants;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Validators;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Registration.UseCases;

public class ValidateNewPerson(IUnitOfWorkFactory uowFactory) : IValidateNewPerson
{
    private const string MethodName = nameof(ValidateNewPerson);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

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
        if (!IdentityDocumentRules.IsNationalId(documentType))
        {
            return Task.FromResult(OperationResult<object?>.IsFailed(
                RegistrationErrorCodes.UnsupportedDocumentType,
                MethodName,
                "ConfirmNewPerson solo aplica para cédula de identidad.",
                400));
        }

        using var uow = _uowFactory.Create();

        var document = TextNormalization.Trim(request.DocumentNumber);
        var person = uow.Personas.GetByDocumento(document);
        if (person != null)
        {
            return Task.FromResult(OperationResult<object?>.IsFailed(
                "REG_PERSONA_02",
                MethodName,
                "La persona ya existe.",
                409));
        }

        var city = uow.Ciudads.GetByKey(request.CountryId, request.StateId, request.CityId);
        if (city == null)
        {
            return Task.FromResult(OperationResult<object?>.IsFailed(
                "REG_CIUDAD_01",
                MethodName,
                "No existe la ciudad indicada.",
                400));
        }

        return Task.FromResult(OperationResult<object?>.IsSuccess(
            null,
            MethodName,
            "Validación correcta."));
    }
}
