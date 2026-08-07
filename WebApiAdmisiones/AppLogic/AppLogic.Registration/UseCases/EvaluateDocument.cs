using AppLogic.Contracts.Text;
using AppLogic.Identity;
using AppLogic.Registration.Constants;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Services;
using AppLogic.Registration.Validators;
using BusinessLogic.IDevartRepositories;
using System.Globalization;
using Utilities;

namespace AppLogic.Registration.UseCases;

public class EvaluateDocument(IUnitOfWorkFactory uowFactory, LdapUserDirectory ldapDirectory) : IEvaluateDocument
{
    private const string MethodName = nameof(EvaluateDocument);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly LdapUserDirectory _ldapDirectory = ldapDirectory;

    public async Task<OperationResult<DocumentEvaluationResponse>> ExecuteAsync(EvaluateDocumentRequest request)
    {
        if (request == null)
        {
            return OperationResult<DocumentEvaluationResponse>.IsFailed(
                RegistrationErrorCodes.MissingRequest,
                MethodName,
                RegistrationErrorCodes.MissingRequestMessage,
                400);
        }

        var validation = IdentityDocumentRules.ValidateBaseDocument(request.DocumentType, request.DocumentNumber);
        if (!validation.IsValid)
        {
            return OperationResult<DocumentEvaluationResponse>.IsFailed(
                RegistrationValidation.ResolveDocumentValidationCode(validation.Error),
                MethodName,
                validation.Message,
                400);
        }

        var documentType = TextNormalization.Trim(request.DocumentType);
        var document = TextNormalization.Trim(request.DocumentNumber);

        using var uow = _uowFactory.Create();
        if (!IdentityDocumentRules.IsNationalId(documentType))
        {
            var registrationRequest = uow.SolicitudAltas.GetByTipoDocumentoYDocumento(documentType, document);
            if (registrationRequest != null)
            {
                return OperationResult<DocumentEvaluationResponse>.IsSuccess(
                    new DocumentEvaluationResponse
                    {
                        RegistrationRequestPending = true
                    },
                    MethodName,
                    "El documento ingresado está en revisión.");
            }

            return OperationResult<DocumentEvaluationResponse>.IsSuccess(
                new DocumentEvaluationResponse
                {
                    RequiresRegistrationRequest = true
                },
                MethodName,
                "No existe solicitud de alta para el documento indicado. Se puede continuar con la solicitud de alta.");
        }

        var person = uow.Personas.GetByTipoDocumentoYDocumento(documentType, document);
        if (person == null)
        {
            return OperationResult<DocumentEvaluationResponse>.IsSuccess(
                new DocumentEvaluationResponse
                {
                    RequiresPersonRegistration = true
                },
                MethodName,
                "La persona no existe. Se puede continuar con el alta.");
        }

        var userExists = await _ldapDirectory.UserExistsAsync(
            person.CodigoPersona.ToString(CultureInfo.InvariantCulture));
        if (userExists)
        {
            return OperationResult<DocumentEvaluationResponse>.IsSuccess(
                new DocumentEvaluationResponse
                {
                    UserAlreadyRegistered = true
                },
                MethodName,
                "La cedula ingresada ya está registrada.");
        }

        return OperationResult<DocumentEvaluationResponse>.IsSuccess(
            new DocumentEvaluationResponse
            {
                RequiresIdentityVerification = true
            },
            MethodName,
            "La persona existe y requiere verificación de apellido y correo.");
    }
}
