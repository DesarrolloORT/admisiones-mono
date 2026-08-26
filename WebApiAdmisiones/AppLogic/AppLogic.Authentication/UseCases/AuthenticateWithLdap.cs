using AppLogic.Contracts;
using AppLogic.Authentication.Contracts;
using AppLogic.Authentication.Rules;
using AppLogic.Contracts.Text;
using AppLogic.Identity;
using AppLogic.Identity.Dtos;
using AppLogic.Identity.Services;
using BusinessLogic.IDevartRepositories;
using LdapService.Interfaces;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.Authentication.UseCases;

public class AuthenticateWithLdap(
    ILdap ldap,
    IUnitOfWorkFactory uowFactory,
    ILogger<AuthenticateWithLdap> logger) : IAuthenticateWithLdap
{
    private const string MethodName = nameof(AuthenticateWithLdap);
    private const string LoginRejectedLog = "Login rechazado ({Motivo}) para la persona {CodigoPersona}";

    private readonly ILdap _ldap = ldap;
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly ILogger<AuthenticateWithLdap> _logger = logger;

    private static OperationResult<AuthenticatedPerson> InvalidCredentials() =>
        OperationResult<AuthenticatedPerson>.IsFailed(
            "AUTH_LDAP_03",
            MethodName,
            "Credenciales inválidas.",
            401,
            default!);

    public async Task<OperationResult<AuthenticatedPerson>> ExecuteAsync(string documentType, string document, string pass)
    {
        try
        {
            var validation = IdentityDocumentRules.ValidateBaseDocument(documentType, document);
            if (!validation.IsValid)
            {
                return OperationResult<AuthenticatedPerson>.IsFailed(
                    IdentityDocumentService.ResolveDocumentValidationCode(validation.Error, "LOGIN_LDAP_02", "LOGIN_LDAP_03"),
                    MethodName,
                    validation.Message,
                    400,
                    default!);
            }

            var normalizedDocumentType = TextNormalization.Trim(documentType);
            var normalizedDocument = TextNormalization.Trim(document);

            using var uow = _uowFactory.Create();
            var person = uow.Personas.GetByTipoDocumentoYDocumento(normalizedDocumentType, normalizedDocument);

            if (person == null)
            {
                _logger.LogWarning(LoginRejectedLog, "persona-inexistente", "n/d");
                return InvalidCredentials();
            }

            if (TextNormalization.IsYes(person.AlumnoExtranjeroPersona))
            {
                _logger.LogWarning(LoginRejectedLog, "alumno-extranjero", person.CodigoPersona);
                return InvalidCredentials();
            }

            var authResult = await _ldap.AutenticarUsuarioLDAPAsync(person.CodigoPersona, pass);

            if (!authResult.Success)
            {
                _logger.LogWarning(
                    "Login rechazado (ldap-rechazo:{ErrorCode}) para la persona {CodigoPersona}",
                    authResult.ErrorCode,
                    person.CodigoPersona);
                return InvalidCredentials();
            }

            return OperationResult<AuthenticatedPerson>.Ok(
                AuthenticationResponseBuilder.BuildAuthenticatedPerson(person),
                MethodName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AuthenticationLogs.UnexpectedError, MethodName);
            return OperationResult<AuthenticatedPerson>.IsFailed(
                "LOGIN_LDAP_99",
                MethodName,
                "Error al autenticar usuario.",
                500,
                default!);
        }
    }
}
