using AppLogic.DTOs;
using AppLogic.IServices.Tivenos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services.Tivenos
{
    public class TivenosEnvioService : ITivenosEnvioService
    {
        public OperationResult<bool> EncolarAltaInteresXSeleccionEnSitio(
            IUnitOfWork uow,
            TivenosAltaInteresRequest request,
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
            TivenosBachilleratoRequest request,
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
            TivenosBachilleratoRequest request,
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

        private static EnvioParaTiveno CrearEnvioAltaInteres(TivenosAltaInteresRequest request)
        {
            return new EnvioParaTiveno
            {
                OrigenLlamador = request.Operacion.OrigenLlamador,
                Origen = "ADMISIONES",
                TipoProcesoLlamador = request.Operacion.TipoProcesoLlamador,
                Disparador = request.Operacion.Disparador,
                Modulo = "InteresProducto",
                Metodo = "AltaInteresXSeleccionEnSitio",
                Status = "Nuevo",
                CodigoSape = request.CodigoPersona,
                ProcesoId = request.IdProceso,
                ProductoId = request.IdProducto,
                InteresProdGradoInteresId = 4,
                MotivodesinteresId = null,
                MotivodesinteresNombre = string.Empty,
            };
        }

        private static EnvioParaTiveno CrearEnvioBachillerato(
            TivenosBachilleratoRequest request,
            string tipoProcesoLlamador,
            string disparador,
            string metodo)
        {
            return new EnvioParaTiveno
            {
                Origen = "ADMISIONES",
                TipoProcesoLlamador = tipoProcesoLlamador,
                Disparador = disparador,
                Modulo = "Bachillerato",
                Metodo = metodo,
                Status = "Nuevo",
                CodigoSape = request.CodigoPersona,
                BachilleratoOrientacionId = request.CodigoOrientacion,
            };
        }
    }
}
