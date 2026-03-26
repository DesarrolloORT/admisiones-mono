using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
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

        public OperationResult<DtoDatosPreInscripcion> ObtenerDatosPreInscripcion(long codigoPersona)
        {
            using var uow = _uowFactory.Create();
            var encuesta = uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
            if (encuesta == null)
            {
                return OperationResult<DtoDatosPreInscripcion>.IsFailed(
                    "PRE_DPI_01",
                    nameof(ObtenerDatosPreInscripcion),
                    "No se encontraron datos de preinscripción para la persona.",
                    204);
            }

            var fechaVencimiento = encuesta.FechaVtoAdmision;
            if (!fechaVencimiento.HasValue && encuesta.IdProceso.HasValue)
            {
                var fechaVencimientoResult = _generalService.CalcularFechaVencimientoAdmisiones(codigoPersona, encuesta.IdProceso.Value);
                if (!fechaVencimientoResult.Success)
                {
                    return OperationResult<DtoDatosPreInscripcion>.IsFailed(
                        fechaVencimientoResult.ErrorCode,
                        nameof(ObtenerDatosPreInscripcion),
                        fechaVencimientoResult.Message,
                        fechaVencimientoResult.HttpCode);
                }

                fechaVencimiento = fechaVencimientoResult.Data;
            }

            DtoOfertaDevart? oferta = null;
            if (encuesta.IdProducto.HasValue && encuesta.IdProceso.HasValue && encuesta.IdTurno.HasValue)
            {
                var ofertas = uow.Ofertas.GetOfertasParaInscripcionConProceso(
                    encuesta.IdProducto.Value,
                    encuesta.IdProceso.Value,
                    encuesta.IdTurno.Value);

                if (ofertas.Count == 1)
                {
                    oferta = ofertas.First().ToDtoWithRelated(1);
                }
            }

            var dto = new DtoDatosPreInscripcion
            {
                IdProceso = encuesta.IdProceso ?? 0,
                NombreProceso = encuesta.Proceso?.NombreProceso,
                IdComienzo = encuesta.IdComienzo ?? 0,
                NombreComienzo = encuesta.Comienzo?.NombreComienzo,
                IdProducto = encuesta.IdProducto ?? 0,
                NombreExtensoProducto = encuesta.Producto?.NombreExtensoProducto ?? encuesta.Producto?.NombreProducto,
                ObjTurno = encuesta.Turno?.ToDto(),
                TipoInscripcion = encuesta.TipoInscripcion,
                FechaVencimiento = fechaVencimiento ?? DateTime.MinValue,
                ObjOferta = oferta
            };

            return OperationResult<DtoDatosPreInscripcion>.Ok(dto, nameof(ObtenerDatosPreInscripcion));
        }
    }
}
