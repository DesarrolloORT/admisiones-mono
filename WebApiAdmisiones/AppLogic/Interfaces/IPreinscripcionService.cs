using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;

namespace AppLogic.Interfaces
{
    public interface IPreinscripcionService
    {
        OperationResult<IEnumerable<DtoProcesoDevart>> ObtenerProcesosHabilitadosPorProducto(long idProducto);
        OperationResult<IEnumerable<DtoTurnoDevart>> ObtenerTurnos(long idProducto, long idProceso);
        OperationResult<IEnumerable<DtoOfertaDevart>> ObtenerOfertasParaInscripcionConProceso(long idProducto, long idProceso, long idTurno);
        OperationResult<DateTime> ObtenerFechaVencimientoAdmisiones(long codigoPersona, long idProceso);
        OperationResult<DTODatosPreInscripcion> ObtenerDatosPreInscripcion(long codigoPersona);
    }
}
