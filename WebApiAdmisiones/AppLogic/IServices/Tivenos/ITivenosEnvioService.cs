using AppLogic.Dtos.Tivenos;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.IServices.Tivenos
{
    public interface ITivenosEnvioService
    {
        OperationResult<bool> EncolarAltaInteresXSeleccionEnSitio(
            IUnitOfWork uow,
            DtoTivenosAltaInteresRequest request,
            int idTivenos,
            string methodName);

        OperationResult<bool> EncolarAltaDatosBachillerato(
            IUnitOfWork uow,
            DtoTivenosBachilleratoRequest request,
            int idTivenos,
            string methodName);

        OperationResult<bool> EncolarModificacionDatosBachillerato(
            IUnitOfWork uow,
            DtoTivenosBachilleratoRequest request,
            int idTivenos,
            string methodName);
    }
}
