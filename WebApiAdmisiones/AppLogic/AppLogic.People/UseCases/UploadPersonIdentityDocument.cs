using AppLogic.Contracts;
using AppLogic.Identity.Constants;
using AppLogic.Identity.Dtos;
using AppLogic.Identity.Services;
using AppLogic.People.Constants;
using AppLogic.People.Contracts;
using AppLogic.People.Rules;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.People.UseCases;

public class UploadPersonIdentityDocument(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext) : IUploadPersonIdentityDocument
{
    private const string MethodName = nameof(UploadPersonIdentityDocument);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;

    public OperationResult<bool> Execute(
        long personId,
        DateTime fecha,
        IdentityDocumentFile frente,
        IdentityDocumentFile dorso)
    {
        var frontValidation = ValidateFile(frente, "frente");
        if (!frontValidation.Success)
        {
            return frontValidation;
        }

        var backValidation = ValidateFile(dorso, "dorso");
        if (!backValidation.Success)
        {
            return backValidation;
        }

        var dateValidation = IdentityDocumentService.ValidateDocumentExpiration(fecha, MethodName, "GEN_SDA_05");
        if (!dateValidation.Success)
        {
            return dateValidation;
        }

        using var uow = _uowFactory.Create();

        var person = uow.Personas.GetByKey(personId);
        if (person is null)
        {
            return OperationResult<bool>.IsFailed("GEN_SDA_02", MethodName, PersonConstants.PersonaNoEncontradaMessage, 404);
        }

        var frontResult = SaveOrUpdate(uow, personId, IdentityDocumentConstants.ImageType.Front, fecha, frente);
        if (!frontResult.Success)
        {
            return frontResult;
        }

        var backResult = SaveOrUpdate(uow, personId, IdentityDocumentConstants.ImageType.Back, fecha, dorso);
        if (!backResult.Success)
        {
            return backResult;
        }

        person.FechaVtoDocumentoPersona = fecha;
        PersonAuditStamp.Apply(person, personId, uow);
        uow.Save();
        return OperationResult<bool>.Ok(true, MethodName);
    }

    private OperationResult<bool> SaveOrUpdate(
        IUnitOfWork uow,
        long personId,
        int tipo,
        DateTime fecha,
        IdentityDocumentFile document)
    {
        var fileContent = document.Content ?? Array.Empty<byte>();
        var fileName = document.FileName ?? string.Empty;
        var existingDocument = uow.ImagenTemporals.GetDocumentoByPersonaAndTipo(personId, tipo);

        if (existingDocument is null)
        {
            var saveResult = IdentityDocumentService.CreateTemporaryDocument(
                personId,
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN_TEMPORAL),
                tipo,
                fecha,
                fileContent,
                fileName,
                MethodName);

            if (!saveResult.Success)
                return saveResult.Failure().As<bool>(MethodName);

            uow.ImagenTemporals.Add(saveResult.Data!);
        }
        else
        {
            var updateResult = IdentityDocumentService.UpdateTemporaryDocument(
                existingDocument,
                tipo,
                fecha,
                fileContent,
                fileName,
                MethodName);
            if (!updateResult.Success)
                return updateResult.Failure().As<bool>(MethodName);

            uow.ImagenTemporals.Update(existingDocument);
        }

        return OperationResult<bool>.Ok(true, MethodName);
    }

    private static OperationResult<bool> ValidateFile(IdentityDocumentFile? document, string lado)
    {
        if (document?.Content == null || document.Content.Length == 0)
        {
            return OperationResult<bool>.IsFailed(
                "GEN_SDA_03",
                MethodName,
                $"Debe enviar el documento de identidad ({lado}).",
                400);
        }

        if (string.IsNullOrWhiteSpace(document.FileName))
        {
            return OperationResult<bool>.IsFailed(
                "GEN_SDA_04",
                MethodName,
                $"Debe enviar el nombre del archivo del documento de identidad ({lado}).",
                400);
        }

        var validation = FileValidator.ValidateImageFile(document.Content, document.FileName, MethodName);
        if (!validation.Success)
            return validation.Failure().As<bool>(MethodName);

        return OperationResult<bool>.Ok(true, MethodName);
    }
}
