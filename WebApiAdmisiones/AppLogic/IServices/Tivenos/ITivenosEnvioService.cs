using AppLogic.DTOs;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.IServices.Tivenos
{
    public interface ITivenosEnvioService
    {
        OperationResult<bool> EncolarAltaInteresXSeleccionEnSitio(
            IUnitOfWork uow,
            TivenosAltaInteresRequest request,
            int idTivenos,
            string methodName);
    }
}
