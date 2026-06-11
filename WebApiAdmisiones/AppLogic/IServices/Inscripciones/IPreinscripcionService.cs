using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices.Inscripciones
{
    public interface IPreinscripcionService
    {
        OperationResult<DateTime> ObtenerFechaVencimientoAdmisiones(long codigoPersona, long idProceso);
        //OperationResult<DtoDatosPreInscripcion> ObtenerDatosPreInscripcion(long codigoPersona);
    }
}
