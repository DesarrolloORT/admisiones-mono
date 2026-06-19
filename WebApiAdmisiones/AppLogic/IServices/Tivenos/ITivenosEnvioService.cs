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

        OperationResult<bool> EncolarAltaDatosBachillerato(
            IUnitOfWork uow,
            TivenosBachilleratoRequest request,
            int idTivenos,
            string methodName);

        OperationResult<bool> EncolarModificacionDatosBachillerato(
            IUnitOfWork uow,
            TivenosBachilleratoRequest request,
            int idTivenos,
            string methodName);
    }
}
