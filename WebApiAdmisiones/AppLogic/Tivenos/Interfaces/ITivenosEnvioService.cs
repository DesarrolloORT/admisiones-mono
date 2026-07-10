using AppLogic.Tivenos.Dtos;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Tivenos.Interfaces
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
