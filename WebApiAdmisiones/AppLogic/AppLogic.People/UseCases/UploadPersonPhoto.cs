using AppLogic.Contracts;
using AppLogic.Identity.Services;
using AppLogic.People.Constants;
using AppLogic.People.Contracts;
using AppLogic.People.Rules;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.People.UseCases;

public class UploadPersonPhoto(
    IUnitOfWorkFactory uowFactory,
    IDbConnectionContext dbConnectionContext) : IUploadPersonPhoto
{
    private const string MethodName = nameof(UploadPersonPhoto);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IDbConnectionContext _dbConnectionContext = dbConnectionContext;

    public OperationResult<bool> Execute(long personId, byte[] fileContent, string fileName)
    {
        using var uow = _uowFactory.Create();

        var person = uow.Personas.GetByKey(personId);
        if (person is null)
        {
            return OperationResult<bool>.IsFailed("GEN_SFA_01", MethodName, PersonConstants.PersonaNoEncontradaMessage, 404);
        }

        var existingImage = uow.Imagens.GetFotoByPersona(personId);

        if (existingImage is null)
        {
            var saveResult = CreatePhoto(
                person,
                _dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_IMAGEN),
                fileContent,
                fileName);

            if (!saveResult.Success)
                return saveResult.Failure().As<bool>(MethodName);

            uow.Imagens.Add(saveResult.Data!);
        }
        else
        {
            var updateResult = ReplacePhoto(existingImage, fileContent, fileName);
            if (!updateResult.Success)
                return updateResult.Failure().As<bool>(MethodName);

            uow.Imagens.Update(existingImage);
        }

        PersonAuditStamp.Apply(person, personId, uow);
        uow.Save();
        return OperationResult<bool>.Ok(true, MethodName);
    }

    private static OperationResult<Imagen> CreatePhoto(Persona person, int idImagen, byte[] fileContent, string fileName)
    {
        const string origen = "SavePersonPhoto";

        if (fileContent == null || fileContent.Length == 0)
            return OperationResult<Imagen>.IsFailed("GEN_SFA_03", origen, "La imagen no puede estar vacía.", 400);

        var imageValidation = FileValidator.ValidateImageFile(fileContent, fileName, origen);
        if (!imageValidation.Success)
        {
            return OperationResult<Imagen>.IsFailed(
                "GEN_SFA_02",
                origen,
                $"La imagen no es válida. Solo se permiten imágenes válidas en formato JPG, JPEG o PNG. Detalle: {imageValidation.Message}",
                400);
        }

        var newPhoto = new Imagen
        {
            IdImagen = idImagen,
            CodigoPersona = person.CodigoPersona
        };
        IdentityDocumentService.ApplyPhotoData(newPhoto, person.CodigoPersona, fileContent, fileName);

        return OperationResult<Imagen>.Ok(newPhoto, origen);
    }

    private static OperationResult<bool> ReplacePhoto(Imagen existing, byte[] fileContent, string fileName)
    {
        const string origen = "ModificarFotoPersona";

        if (fileContent == null || fileContent.Length == 0)
            return OperationResult<bool>.IsFailed("GEN_SFA_04", origen, "La imagen no puede estar vacía.", 400);

        var imageValidation = FileValidator.ValidateImageFile(fileContent, fileName, origen);
        if (!imageValidation.Success)
        {
            return OperationResult<bool>.IsFailed(
                "GEN_SFA_05",
                origen,
                $"La imagen no es válida. Solo se permiten imágenes válidas en formato JPG, JPEG o PNG. Detalle: {imageValidation.Message}",
                400);
        }

        IdentityDocumentService.ApplyPhotoData(existing, existing.CodigoPersona ?? 0, fileContent, fileName);
        return OperationResult<bool>.Ok(true, origen);
    }
}
