using AppLogic.DTOs;
using AppLogic.IServices.Tivenos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services.Tivenos
{
    public class TivenosEnvioService : ITivenosEnvioService
    {
        private const long CodigoPersonaPruebaLegacy = 129329;

        public OperationResult<bool> EncolarAltaInteresXSeleccionEnSitio(
            IUnitOfWork uow,
            TivenosAltaInteresRequest request,
            int idTivenos,
            string methodName)
        {
            ArgumentNullException.ThrowIfNull(uow);
            ArgumentNullException.ThrowIfNull(request);

            if (!DebeEncolar(uow, request.CodigoPersona))
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            var envio = CrearEnvioAltaInteres(request);
            envio.IdEnvioParaTivenos = idTivenos;

            uow.EnvioParaTivenos.Add(envio);

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static bool DebeEncolar(IUnitOfWork uow, long codigoPersona)
        {
            var seLiberoTivenos = uow.Parametros.ObtenerSeLiberoTivenos();
            if (string.Equals(seLiberoTivenos?.Trim(), "SI", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return codigoPersona == CodigoPersonaPruebaLegacy;
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
    }
}
