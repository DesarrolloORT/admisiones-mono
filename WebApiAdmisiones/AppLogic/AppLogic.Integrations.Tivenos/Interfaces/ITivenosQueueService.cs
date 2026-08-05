using AppLogic.Integrations.Tivenos.Dtos;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.Integrations.Tivenos.Interfaces;

public interface ITivenosQueueService
{
    bool EnqueueProductInterestFromSiteSelection(
        IUnitOfWork uow,
        DtoTivenosAltaInteresRequest request,
        int idTivenos);

    bool EnqueueHighSchoolDataCreation(
        IUnitOfWork uow,
        DtoTivenosBachilleratoRequest request,
        int idTivenos);

    bool EnqueueHighSchoolDataUpdate(
        IUnitOfWork uow,
        DtoTivenosBachilleratoRequest request,
        int idTivenos);
}
