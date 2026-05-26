using AppLogic.DTOs;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.IServices
{
    public interface IInscripcionesService
    {
        OperationResult<DtoUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona);
        OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosVigentesConInteres(long codigoPersona);
        OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosConInteresActivo(long codigoPersona);
        OperationResult<bool> RegistrarInteresProducto(long codigoPersona, InteresProductoRequest request);
        OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso);
        OperationResult<IEnumerable<DtoInscripcionHome>> ObtenerMisInscripciones(long codigoPersona);
        OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso);
        OperationResult<bool> TieneDerechoAEncuestaInicial(long codigoPersona);
    }
}
