using AppLogic.DTOs;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.Interfaces
{
    public interface IInscripcionesService
    {
        OperationResult<DTOUltimaInscripcion> ObtenerUltimaInscripcionActiva(long codigoPersona);
        OperationResult<IEnumerable<DTOProductoAdmisiones>> ObtenerProductosVigentesConInteres(long codigoPersona);
        OperationResult<IEnumerable<DTOProductoAdmisiones>> ObtenerProductosConInteresActivo(long codigoPersona);
        OperationResult<bool> TieneInscripcionActivaParaProceso(long codigoPersona, long idProducto, long idProceso);
        OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesPendientes(long codigoPersona);
        OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>> ObtenerInscripcionesCanceladas(long codigoPersona);
        OperationResult<IEnumerable<DTOInscripcionRealizada>> ObtenerInscripcionesRealizadas(long codigoPersona);
        OperationResult<bool> TieneInscripcionAdmisiones(long codigoPersona, long idProducto, long idProceso);
    }
}
