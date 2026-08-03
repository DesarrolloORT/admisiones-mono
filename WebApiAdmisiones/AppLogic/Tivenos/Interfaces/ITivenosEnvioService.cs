using AppLogic.Tivenos.Dtos;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.Tivenos.Interfaces;

public interface ITivenosEnvioService
{
    bool EncolarAltaInteresXSeleccionEnSitio(
        IUnitOfWork uow,
        DtoTivenosAltaInteresRequest request,
        int idTivenos);

    bool EncolarAltaDatosBachillerato(
        IUnitOfWork uow,
        DtoTivenosBachilleratoRequest request,
        int idTivenos);

    bool EncolarModificacionDatosBachillerato(
        IUnitOfWork uow,
        DtoTivenosBachilleratoRequest request,
        int idTivenos);
}
