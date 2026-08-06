using AppLogic.Contracts;
using AppLogic.Contracts.Text;
using AppLogic.Identity.Dtos;
using AppLogic.Identity.Services;
using AppLogic.Registration.Constants;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Mapping;
using AppLogic.Registration.Services;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Utilities;

namespace AppLogic.Registration.UseCases;

/// <summary>
/// Alta completa de una persona nueva con los datos que quedaron en Redis. Son dos transacciones
/// de base con el alta de LDAP en el medio: ver los comentarios de cada paso.
/// </summary>
public class CompleteNewPerson(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext,
    LdapUserDirectory ldapDirectory,
    ILogger<CompleteNewPerson> logger) : ICompleteNewPerson
{
    private const string MethodName = nameof(CompleteNewPerson);
    private const string ErrorInesperadoLog = "Error inesperado en {Metodo}";

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;
    private readonly LdapUserDirectory _ldapDirectory = ldapDirectory;
    private readonly ILogger<CompleteNewPerson> _logger = logger;

    public async Task<OperationResult<long>> ExecuteAsync(
        PendingPerson data,
        string newPassword,
        TemporaryDocumentImages? images = null)
    {
        if (data == null)
        {
            return OperationResult<long>.IsFailed(
                RegistrationErrorCodes.MissingRequest,
                MethodName,
                RegistrationErrorCodes.MissingRequestMessage,
                400,
                default);
        }

        var passwordValidation = Util.ValidarPasswordNueva(newPassword);
        if (!string.IsNullOrWhiteSpace(passwordValidation))
        {
            return OperationResult<long>.IsFailed(
                "INI_PAS_02",
                MethodName,
                passwordValidation,
                400,
                default);
        }

        using var uow = _uowFactory.Create();
        var documentType = TextNormalization.Trim(data.DocumentType);
        var document = TextNormalization.Trim(data.DocumentNumber);
        var existingPerson = uow.Personas.GetByDocumento(document);
        if (existingPerson != null)
        {
            if (!IsSamePendingPerson(existingPerson, data, documentType, document))
            {
                return OperationResult<long>.IsFailed(
                    "REG_PERSONA_02",
                    MethodName,
                    "La persona ya existe.",
                    409,
                    default);
            }

            return await CompletePasswordForPendingExistingPersonAsync(uow, existingPerson, newPassword);
        }

        var city = uow.Ciudads.GetByKey(data.CountryId, data.StateId, data.CityId);
        if (city == null)
        {
            return OperationResult<long>.IsFailed(
                "REG_CIUDAD_01",
                MethodName,
                "No existe la ciudad indicada.",
                400,
                default);
        }

        var imagesValidation = IdentityDocumentService.ValidateRecognizedDocumentImages(images, MethodName);
        if (!imagesValidation.Success)
        {
            return imagesValidation.Failure().As<long>(MethodName);
        }

        var createPersonResult = CreatePersonInDb(uow, data, city);
        if (!createPersonResult.Success)
        {
            return createPersonResult.Failure().As<long>(MethodName);
        }

        var person = createPersonResult.Data!;

        // LDAP fuera de la transacción DB: la persona ya está commiteada, así que un
        // fallo acá no la revierte. Si queda huérfana (sin usuario LDAP o sin password),
        // el reintento la encuentra vía GetByDocumento y sigue por CompletePasswordForPendingExistingPersonAsync.
        var createUser = await _ldapDirectory.CreateUserAsync(RegistrationMapper.BuildLdapUserRequest(person));
        if (!createUser.Success)
        {
            _logger.LogError(
                "Estado inconsistente: persona {CodigoPersona} creada en DB pero sin usuario LDAP ({ErrorCode}).",
                person.CodigoPersona,
                createUser.ErrorCode);
            return createUser.Failure().As<long>(MethodName);
        }

        var passwordChange = await _ldapDirectory.ForcePasswordAsync(
            person.CodigoPersona.ToString(CultureInfo.InvariantCulture),
            newPassword);
        if (!passwordChange.Success)
        {
            _logger.LogError(
                "Estado inconsistente: persona {CodigoPersona} con usuario LDAP creado pero sin password establecida ({ErrorCode}).",
                person.CodigoPersona,
                passwordChange.ErrorCode);
            return passwordChange.Failure().As<long>(MethodName);
        }

        var persistenciaResult = PersistMetadataAndAdmission(uow, person, images);
        if (!persistenciaResult.Success)
        {
            return persistenciaResult.Failure().As<long>(MethodName);
        }

        return OperationResult<long>.Ok(person.CodigoPersona, MethodName);
    }

    /// <summary>Tx #1: crea la Persona en DB (commit propio, LDAP viene después fuera de esta transacción).</summary>
    private OperationResult<Persona> CreatePersonInDb(IUnitOfWork uow, PendingPerson data, Ciudad city)
    {
        try
        {
            uow.BeginTransaction();
            var person = RegistrationMapper.CreatePerson(
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_PERSONA),
                data,
                city,
                DateTime.Now);
            uow.Personas.Add(person);
            uow.Save();

            // T_PERSONA.FECHA_CONF_DATOS_PERSONA tiene DEFAULT sysdate en Oracle: se completa sola en el INSERT
            // aunque no la seteemos. Debe quedar vacía para forzar la confirmación de datos en Autoservicio.
            person.FechaConfDatosPersona = null;
            uow.Save();

            uow.Commit();
            return OperationResult<Persona>.Ok(person, MethodName);
        }
        catch (Exception ex)
        {
            uow.Rollback();
            _logger.LogError(ex, ErrorInesperadoLog, MethodName);
            return OperationResult<Persona>.IsFailed(
                "REG_PERSONA_99",
                MethodName,
                "Error al crear la persona.",
                500,
                default!);
        }
    }

    /// <summary>Tx #2: metadata de password, alta de admisión e imágenes, después de que LDAP ya quedó activo.</summary>
    private OperationResult<bool> PersistMetadataAndAdmission(IUnitOfWork uow, Persona person, TemporaryDocumentImages? images)
    {
        try
        {
            uow.BeginTransaction();
            UpdatePasswordMetadata(uow, person);
            AdmissionRecords.AddForPerson(uow, _dbConnectionContext, person.CodigoPersona);
            SaveRecognizedDocumentImages(uow, person, images);
            uow.Commit();
            return OperationResult<bool>.Ok(true, MethodName);
        }
        catch (Exception ex)
        {
            uow.Rollback();
            _logger.LogError(ex,
                "Estado inconsistente: persona {CodigoPersona} con LDAP activo pero metadata/admisión/imágenes sin persistir.",
                person.CodigoPersona);
            return OperationResult<bool>.IsFailed(
                "REG_PERSONA_99",
                MethodName,
                "Error al crear la persona.",
                500,
                default!);
        }
    }

    private void SaveRecognizedDocumentImages(IUnitOfWork uow, Persona person, TemporaryDocumentImages? images)
    {
        if (images is null)
        {
            return;
        }

        IdentityDocumentService.SaveRecognizedDocumentImages(uow, _dbConnectionContext, person, images);
        uow.Personas.Update(person);
    }

    /// <summary>
    /// Reintento del alta: la persona ya se creó en un intento anterior que se cortó antes de dejarle
    /// la contraseña. Solo hay que completarla en LDAP.
    /// </summary>
    private async Task<OperationResult<long>> CompletePasswordForPendingExistingPersonAsync(
        IUnitOfWork uow,
        Persona person,
        string newPassword)
    {
        var passwordChange = await _ldapDirectory.ForcePasswordAsync(
            person.CodigoPersona.ToString(CultureInfo.InvariantCulture),
            newPassword);

        if (!passwordChange.Success)
        {
            return passwordChange.Failure().As<long>(MethodName);
        }

        UpdatePasswordMetadata(uow, person);
        uow.Save();

        return OperationResult<long>.Ok(person.CodigoPersona, MethodName);
    }

    private static bool IsSamePendingPerson(
        Persona person,
        PendingPerson data,
        string documentType,
        string document)
    {
        return TextNormalization.Trim(person.TipoDocumento) == documentType
            && TextNormalization.Trim(person.Documento) == document
            && string.Equals(
                TextNormalization.Trim(person.Email),
                TextNormalization.Trim(data.Email),
                StringComparison.Ordinal);
    }

    private static void UpdatePasswordMetadata(IUnitOfWork uow, Persona person)
    {
        person.FechaUltModifPassword = DateTime.Today;
        person.UsuarioUltModifPassword = Constantes.kUSERNAME_USUARIO_ADMISIONES;
        uow.Personas.Update(person);
    }
}
