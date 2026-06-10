using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Inscripciones;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services.Inscripciones
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

            var fechaVencimientoResult = ObtenerFechaVencimientoEncuesta(codigoPersona, encuesta);
            if (!fechaVencimientoResult.Success)
            {
                return OperationResult<DtoDatosPreInscripcion>.IsFailed(
                    fechaVencimientoResult.ErrorCode,
                    nameof(ObtenerDatosPreInscripcion),
                    fechaVencimientoResult.Message,
                    fechaVencimientoResult.HttpCode);
            }

            var oferta = ObtenerOfertaPreinscripcion(uow, encuesta);

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
                FechaVencimiento = fechaVencimientoResult.Data,
                ObjOferta = oferta
            };

            return OperationResult<DtoDatosPreInscripcion>.Ok(dto, nameof(ObtenerDatosPreInscripcion));
        }

        private OperationResult<DateTime> ObtenerFechaVencimientoEncuesta(long codigoPersona, EncuestaIniAdmision encuesta)
        {
            if (encuesta.FechaVtoAdmision.HasValue)
            {
                return OperationResult<DateTime>.Ok(encuesta.FechaVtoAdmision.Value, nameof(ObtenerDatosPreInscripcion));
            }

            if (!encuesta.IdProceso.HasValue)
            {
                return OperationResult<DateTime>.Ok(DateTime.MinValue, nameof(ObtenerDatosPreInscripcion));
            }

            var fechaVencimientoResult = _generalService.CalcularFechaVencimientoAdmisiones(codigoPersona, encuesta.IdProceso.Value);
            if (!fechaVencimientoResult.Success)
            {
                return OperationResult<DateTime>.IsFailed(
                    fechaVencimientoResult.ErrorCode,
                    nameof(ObtenerDatosPreInscripcion),
                    fechaVencimientoResult.Message,
                    fechaVencimientoResult.HttpCode);
            }

            return OperationResult<DateTime>.Ok(fechaVencimientoResult.Data, nameof(ObtenerDatosPreInscripcion));
        }

        private static DtoOfertaDevart? ObtenerOfertaPreinscripcion(IUnitOfWork uow, EncuestaIniAdmision encuesta)
        {
            if (!encuesta.IdProducto.HasValue || !encuesta.IdProceso.HasValue || !encuesta.IdTurno.HasValue)
            {
                return null;
            }

            var ofertas = uow.Ofertas.GetOfertasParaInscripcionConProceso(
                encuesta.IdProducto.Value,
                encuesta.IdProceso.Value,
                encuesta.IdTurno.Value);

            return ofertas.Count == 1
                ? ofertas.First().ToDtoWithRelated(1)
                : null;
        }
    }
}
