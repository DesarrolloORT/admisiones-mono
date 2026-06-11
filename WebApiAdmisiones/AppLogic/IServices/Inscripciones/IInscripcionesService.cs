using AppLogic.DTOs;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.IServices.Inscripciones
{
    public interface IInscripcionesService
    {
        OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>> ObtenerMisInscripciones(long codigoPersona);
        OperationResult<DtoUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona);
        OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosVigentesConInteres(long codigoPersona);
        OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosConInteresActivo(long codigoPersona);
        OperationResult<bool> RegistrarInteresProducto(long codigoPersona, InteresProductoRequest request);
        OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso);
        OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso);
        OperationResult<bool> TieneDerechoAEncuestaInicial(long codigoPersona);
    }
}
