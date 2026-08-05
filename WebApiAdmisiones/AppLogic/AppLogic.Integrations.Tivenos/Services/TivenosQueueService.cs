using AppLogic.Integrations.Tivenos.Dtos;
using AppLogic.Integrations.Tivenos.Mapping;
using AppLogic.Integrations.Tivenos.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.Integrations.Tivenos.Services;

public class TivenosQueueService : ITivenosQueueService
{

    public bool EnqueueProductInterestFromSiteSelection(
        IUnitOfWork uow,
        DtoTivenosAltaInteresRequest request,
        int idTivenos)
    {
        ArgumentNullException.ThrowIfNull(uow);
        ArgumentNullException.ThrowIfNull(request);

        var envio = TivenosMessageMapper.ToProductInterestMessage(request);
        envio.IdEnvioParaTivenos = idTivenos;

        uow.EnvioParaTivenos.Add(envio);

        return true;
    }

    public bool EnqueueHighSchoolDataCreation(
        IUnitOfWork uow,
        DtoTivenosBachilleratoRequest request,
        int idTivenos)
    {
        ArgumentNullException.ThrowIfNull(uow);
        ArgumentNullException.ThrowIfNull(request);

        var envio = TivenosMessageMapper.ToHighSchoolMessage(
            request,
            "Alta",
            "AltaBachilleratoPersona",
            "AltaDatosBachillerato");
        envio.IdEnvioParaTivenos = idTivenos;

        uow.EnvioParaTivenos.Add(envio);

        return true;
    }

    public bool EnqueueHighSchoolDataUpdate(
        IUnitOfWork uow,
        DtoTivenosBachilleratoRequest request,
        int idTivenos)
    {
        ArgumentNullException.ThrowIfNull(uow);
        ArgumentNullException.ThrowIfNull(request);

        var envio = TivenosMessageMapper.ToHighSchoolMessage(
            request,
            "Modificacion",
            "ModificacionBachilleratoPersona",
            "ModificacionDatosBachillerato");
        envio.IdEnvioParaTivenos = idTivenos;

        uow.EnvioParaTivenos.Add(envio);

        return true;
    }

}
