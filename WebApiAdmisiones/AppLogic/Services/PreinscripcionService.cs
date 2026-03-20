using AppLogic.DevartDTOs;
using AppLogic.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services
{
    public class PreinscripcionService : IPreinscripcionService
    {
        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly IGeneralService _generalService;

        public PreinscripcionService(IUnitOfWorkFactory uowFactory, IGeneralService generalService)
        {
            _uowFactory = uowFactory;
            _generalService = generalService;
        }

        public OperationResult<IEnumerable<DtoProcesoDevart>> ObtenerProcesosHabilitadosPorProducto(long idProducto)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Procesos.GetProcesosHabilitadosPorProducto(idProducto);
            return OperationResult<IEnumerable<DtoProcesoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerProcesosHabilitadosPorProducto));
        }

        public OperationResult<IEnumerable<DtoTurnoDevart>> ObtenerTurnos(long idProducto, long idProceso)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Turnos.GetTurnosParaAdmisiones(idProducto, idProceso);
            return OperationResult<IEnumerable<DtoTurnoDevart>>.Ok(entidades.ToDtos(), nameof(ObtenerTurnos));
        }

        public OperationResult<IEnumerable<DtoOfertaDevart>> ObtenerOfertasParaInscripcionConProceso(long idProducto, long idProceso, long idTurno)
        {
            using var uow = _uowFactory.Create();
            var entidades = uow.Ofertas.GetOfertasParaInscripcionConProceso(idProducto, idProceso, idTurno);
            return OperationResult<IEnumerable<DtoOfertaDevart>>.Ok(entidades.ToDtosWithRelated(2), nameof(ObtenerOfertasParaInscripcionConProceso));
        }

        public OperationResult<DateTime> ObtenerFechaVencimientoAdmisiones(long codigoPersona, long idProceso)
        {
            var result = _generalService.CalcularFechaVencimientoAdmisiones(codigoPersona, idProceso);
            if (!result.Success)
            {
                return OperationResult<DateTime>.IsFailed(
                    result.ErrorCode,
                    nameof(ObtenerFechaVencimientoAdmisiones),
                    result.Message,
                    result.HttpCode);
            }

            return OperationResult<DateTime>.Ok(result.Data, nameof(ObtenerFechaVencimientoAdmisiones));
        }
    }
}
