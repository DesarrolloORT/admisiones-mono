using AppLogic.Authentication.Interfaces;
using AppLogic.Contracts;
using AppLogic.Contracts.Text;
using AppLogic.Identity;
using AppLogic.Identity.Services;
using AppLogic.Registration.Constants;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Mapping;
using AppLogic.Registration.Services;
using AppLogic.Registration.Validators;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Utilities;

namespace AppLogic.Registration.UseCases;

/// <summary>
/// Registro de una persona que ya está en el padrón: no crea t_persona, solo le da usuario LDAP,
/// alta de admisión y mail de activación.
/// </summary>
public class VerifyIdentity(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext,
    LdapUserDirectory ldapDirectory,
    IPasswordActivationService passwordActivationService,
    ILogger<VerifyIdentity> logger,
    IServiceScopeFactory? serviceScopeFactory = null) : IVerifyIdentity
{
    private const string MethodName = nameof(VerifyIdentity);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;
    private readonly LdapUserDirectory _ldapDirectory = ldapDirectory;
    private readonly IPasswordActivationService _passwordActivationService = passwordActivationService;
    private readonly ILogger<VerifyIdentity> _logger = logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory = serviceScopeFactory;

    private static OperationResult<RegistrationConfirmationResponse?> InconsistentData() =>
        OperationResult<RegistrationConfirmationResponse?>.IsFailed(
            "REG_PERSONA_VERIF_01",
            MethodName,
            "No podemos completar el registro ya que hemos encontrado inconsistencias en los datos ingresados.",
            400);

    public async Task<OperationResult<RegistrationConfirmationResponse?>> ExecuteAsync(VerifyIdentityRequest request)
    {
        if (request == null)
        {
            return OperationResult<RegistrationConfirmationResponse?>.IsFailed(
                RegistrationErrorCodes.MissingRequest,
                MethodName,
                RegistrationErrorCodes.MissingRequestMessage,
                400);
        }

        var documentValidation = IdentityDocumentRules.ValidateBaseDocument(request.DocumentType, request.DocumentNumber);
        if (!documentValidation.IsValid)
        {
            return OperationResult<RegistrationConfirmationResponse?>.IsFailed(
                RegistrationValidation.ResolveDocumentValidationCode(documentValidation.Error),
                MethodName,
                documentValidation.Message,
                400);
        }

        var documentType = TextNormalization.Trim(request.DocumentType);
        var document = TextNormalization.Trim(request.DocumentNumber);
        if (!IdentityDocumentRules.IsNationalId(documentType))
        {
            return OperationResult<RegistrationConfirmationResponse?>.IsFailed(
                RegistrationErrorCodes.UnsupportedDocumentType,
                MethodName,
                "VerifyIdentity solo aplica para cédula de identidad.",
                400);
        }

        using var uow = _uowFactory.Create();
        var person = uow.Personas.GetByDocumento(document);
        if (person == null)
        {
            _logger.LogWarning(
                "Verificación de identidad rechazada en {Metodo}: no existe persona para el documento.",
                MethodName);
            return InconsistentData();
        }

        var userExists = await _ldapDirectory.UserExistsAsync(
            person.CodigoPersona.ToString(CultureInfo.InvariantCulture));
        if (userExists)
        {
            return OperationResult<RegistrationConfirmationResponse?>.IsFailed(
                "REG_USUARIO_01",
                MethodName,
                "La cedula ingresada ya está registrada.",
                409);
        }

        if (!RegistrationValidation.MatchesExistingPerson(person, request))
        {
            return InconsistentData();
        }

        var createUser = await _ldapDirectory.CreateUserAsync(RegistrationMapper.BuildLdapUserRequest(person));
        if (!createUser.Success)
        {
            _logger.LogError(
                "LDAP no pudo crear el usuario de la persona {CodigoPersona} en {Metodo}: {ErrorCode} - {Mensaje}",
                person.CodigoPersona,
                MethodName,
                createUser.ErrorCode,
                createUser.Message);

            return OperationResult<RegistrationConfirmationResponse?>.IsFailed(
                "REG_USUARIO_99",
                MethodName,
                "No pudimos completar el registro. Intentá nuevamente más tarde.",
                500);
        }

        try
        {
            uow.BeginTransaction();
            AdmissionRecords.AddForPerson(uow, _dbConnectionContext, person.CodigoPersona);
            uow.Commit();
        }
        catch (Exception ex)
        {
            uow.Rollback();
            // SRV-01: el usuario LDAP ya se creó (arriba, fuera de esta tx) y no se revierte.
            _logger.LogError(ex,
                "Estado inconsistente: persona {CodigoPersona} con usuario LDAP creado pero admisión sin registrar en {Metodo}.",
                person.CodigoPersona,
                MethodName);
            return OperationResult<RegistrationConfirmationResponse?>.IsFailed(
                "REG_ADMISIONES_99",
                MethodName,
                "Error al registrar la admisión.",
                500);
        }

        return await SendPasswordLinkMailAsync(person);
    }

    private async Task<OperationResult<RegistrationConfirmationResponse?>> SendPasswordLinkMailAsync(Persona person)
    {
        OperationResult<object?> mail;
        if (_serviceScopeFactory != null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedPasswordActivationService = scope.ServiceProvider.GetRequiredService<IPasswordActivationService>();
            mail = await scopedPasswordActivationService.SendPasswordLinkMailAsync(person, MethodName);
        }
        else
        {
            mail = await _passwordActivationService.SendPasswordLinkMailAsync(person, MethodName);
        }

        if (!mail.Success)
        {
            // SRV-05: éxito parcial estructurado, no solo en el mensaje — el front decide qué mostrar.
            _logger.LogWarning(
                "No se pudo enviar el mail de activación para la persona {CodigoPersona} en {Metodo}: {ErrorCode}.",
                person.CodigoPersona,
                MethodName,
                mail.ErrorCode);

            return OperationResult<RegistrationConfirmationResponse?>.IsSuccess(
                new RegistrationConfirmationResponse { MailSent = false },
                MethodName,
                "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de contraseña.");
        }

        return OperationResult<RegistrationConfirmationResponse?>.IsSuccess(
            new RegistrationConfirmationResponse { MailSent = true },
            MethodName,
            "Registro realizado correctamente. Revisá tu casilla de mail para activar tu contraseña.");
    }
}
