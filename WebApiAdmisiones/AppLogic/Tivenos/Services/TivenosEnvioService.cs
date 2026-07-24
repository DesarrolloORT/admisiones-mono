using AppLogic.Tivenos.Dtos;
using AppLogic.Tivenos.Interfaces;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Tivenos.Services;

public class TivenosEnvioService : ITivenosEnvioService
{
    private const string OrigenAdmisiones = "ADMISIONES";
    private const string StatusNuevo = "Nuevo";

    public OperationResult<bool> EncolarAltaInteresXSeleccionEnSitio(
        IUnitOfWork uow,
        DtoTivenosAltaInteresRequest request,
        int idTivenos,
        string methodName)
    {
        ArgumentNullException.ThrowIfNull(uow);
        ArgumentNullException.ThrowIfNull(request);

        var envio = CrearEnvioAltaInteres(request);
        envio.IdEnvioParaTivenos = idTivenos;

        uow.EnvioParaTivenos.Add(envio);

        return OperationResult<bool>.Ok(true, methodName);
    }

    public OperationResult<bool> EncolarAltaDatosBachillerato(
        IUnitOfWork uow,
        DtoTivenosBachilleratoRequest request,
        int idTivenos,
        string methodName)
    {
        ArgumentNullException.ThrowIfNull(uow);
        ArgumentNullException.ThrowIfNull(request);

        var envio = CrearEnvioBachillerato(
            request,
            "Alta",
            "AltaBachilleratoPersona",
            "AltaDatosBachillerato");
        envio.IdEnvioParaTivenos = idTivenos;

        uow.EnvioParaTivenos.Add(envio);

        return OperationResult<bool>.Ok(true, methodName);
    }

    public OperationResult<bool> EncolarModificacionDatosBachillerato(
        IUnitOfWork uow,
        DtoTivenosBachilleratoRequest request,
        int idTivenos,
        string methodName)
    {
        ArgumentNullException.ThrowIfNull(uow);
        ArgumentNullException.ThrowIfNull(request);

        var envio = CrearEnvioBachillerato(
            request,
            "Modificacion",
            "ModificacionBachilleratoPersona",
            "ModificacionDatosBachillerato");
        envio.IdEnvioParaTivenos = idTivenos;

        uow.EnvioParaTivenos.Add(envio);

        return OperationResult<bool>.Ok(true, methodName);
    }

    private static EnvioParaTiveno CrearEnvioAltaInteres(DtoTivenosAltaInteresRequest request)
    {
        return new EnvioParaTiveno
        {
            OrigenLlamador = request.Operacion.OrigenLlamador,
            Origen = OrigenAdmisiones,
            TipoProcesoLlamador = request.Operacion.TipoProcesoLlamador,
            Disparador = request.Operacion.Disparador,
            Modulo = "InteresProducto",
            Metodo = "AltaInteresXSeleccionEnSitio",
            Status = StatusNuevo,
            CodigoSape = request.CodigoPersona,
            ProcesoId = request.IdProceso,
            ProductoId = request.IdProducto,
            InteresProdGradoInteresId = 4,
            MotivodesinteresId = null,
            MotivodesinteresNombre = string.Empty,
        };
    }

    private static EnvioParaTiveno CrearEnvioBachillerato(
        DtoTivenosBachilleratoRequest request,
        string tipoProcesoLlamador,
        string disparador,
        string metodo)
    {
        return new EnvioParaTiveno
        {
            Origen = OrigenAdmisiones,
            TipoProcesoLlamador = tipoProcesoLlamador,
            Disparador = disparador,
            Modulo = "Bachillerato",
            Metodo = metodo,
            Status = StatusNuevo,
            CodigoSape = request.CodigoPersona,
            BachilleratoOrientacionId = request.CodigoOrientacion,
        };
    }
}
