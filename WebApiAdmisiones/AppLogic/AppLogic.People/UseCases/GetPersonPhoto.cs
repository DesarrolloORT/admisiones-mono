using AppLogic.People.Contracts;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.People.UseCases;

public class GetPersonPhoto(IUnitOfWorkFactory uowFactory) : IGetPersonPhoto
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    public OperationResult<byte[]> Execute(long personId)
    {
        const string methodName = nameof(GetPersonPhoto);

        using var uow = _uowFactory.Create();
        var image = uow.Imagens.GetFotoByPersona(personId);

        if (image == null)
            return OperationResult<byte[]>.IsFailed("GEN_FA_01", methodName, "Foto no encontrada.", 404);

        if (image.BlobImagen == null || image.BlobImagen.Length == 0)
            return OperationResult<byte[]>.IsFailed("GEN_FA_02", methodName, "La foto no contiene imagen.", 404);

        return OperationResult<byte[]>.Ok(image.BlobImagen, methodName);
    }
}
