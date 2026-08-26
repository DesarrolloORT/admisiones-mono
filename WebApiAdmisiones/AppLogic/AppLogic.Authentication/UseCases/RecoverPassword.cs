using AppLogic.Authentication.Contracts;
using AppLogic.Authentication.Dtos;
using AppLogic.Authentication.Interfaces;
using AppLogic.Authentication.Rules;
using AppLogic.Contracts.Text;
using AppLogic.Identity;
using AppLogic.Identity.Services;
using BusinessLogic.IDevartRepositories;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.Authentication.UseCases;

/// <summary>
/// Endpoint público: responde siempre el mismo mensaje —incluso ante persona inexistente, datos que
/// no coinciden o excepción— para no permitir enumerar cuentas.
/// </summary>
public class RecoverPassword(
    IUnitOfWorkFactory uowFactory,
    IPasswordActivationService passwordActivationService,
    ILogger<RecoverPassword> logger) : IRecoverPassword
{
    private const string MethodName = nameof(RecoverPassword);

    private const string GenericMessage =
        "Si los datos ingresados son correctos, recibirás un mail con instrucciones para recuperar tu contraseña.";

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IPasswordActivationService _passwordActivationService = passwordActivationService;
    private readonly ILogger<RecoverPassword> _logger = logger;

    public async Task<OperationResult<object>> ExecuteAsync(RecoverPasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return OperationResult<object>.IsFailed(
                    "REC_PAS_01",
                    MethodName,
                    "La solicitud es obligatoria.",
                    400);
            }

            var validation = IdentityDocumentRules.ValidateBaseDocument(request.DocumentType, request.DocumentNumber);
            if (!validation.IsValid)
            {
                return OperationResult<object>.IsFailed(
                    IdentityDocumentService.ResolveDocumentValidationCode(validation.Error, "REC_PAS_02", "REC_PAS_03"),
                    MethodName,
                    validation.Message,
                    400);
            }

            using var uow = _uowFactory.Create();
            var documentType = TextNormalization.Trim(request.DocumentType);
            var document = TextNormalization.Trim(request.DocumentNumber);
            var person = uow.Personas.GetByDocumento(document);

            if (person == null || !PasswordRecoveryValidation.MatchesPerson(person, documentType, document, request.FirstSurname))
            {
                return OperationResult<object>.IsSuccess(null, MethodName, GenericMessage);
            }

            await _passwordActivationService.SendPasswordRecoveryMailAsync(person, MethodName);

            return OperationResult<object>.IsSuccess(null, MethodName, GenericMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AuthenticationLogs.UnexpectedError, MethodName);
            return OperationResult<object>.IsSuccess(null, MethodName, GenericMessage);
        }
    }
}
