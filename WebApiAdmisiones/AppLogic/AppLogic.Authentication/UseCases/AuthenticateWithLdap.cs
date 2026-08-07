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

    private readonly ILdap _ldap = ldap;
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly ILogger<AuthenticateWithLdap> _logger = logger;

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
                return OperationResult<AuthenticatedPerson>.IsFailed(
                    "LOGIN_LDAP_04",
                    MethodName,
                    "No se encontró la persona en la base de datos.",
                    404,
                    default!);
            }

            if (TextNormalization.IsYes(person.AlumnoExtranjeroPersona))
            {
                return OperationResult<AuthenticatedPerson>.IsFailed(
                    "LOGIN_LDAP_05",
                    MethodName,
                    "No se pudo iniciar sesión.",
                    403,
                    default!);
            }

            var authResult = await _ldap.AutenticarUsuarioLDAPAsync(person.CodigoPersona, pass);

            if (!authResult.Success)
            {
                return authResult.Failure().As<AuthenticatedPerson>(MethodName);
            }

            // No se emiten tokens acá: el llamador decide cuándo, según el gate de 2FA.
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
