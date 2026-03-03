using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using System.Collections.Generic;
using Utilities;

namespace AppLogic.Interfaces
{
    public interface IProcesoComienzoServices
    {
        #region PROCESO COMIENZO
        OperationResult<IEnumerable<DtoProcesoComienzoDevart>> ObtenerProcesoComienzos();
        OperationResult<DtoProcesoComienzoDevart> ObtenerProcesoComienzo(long idProceso, long idComienzo);
        OperationResult<DtoProcesoComienzoDevart> GuardarProcesoComienzo(ProcesoComienzoRequest dto);
        OperationResult<DtoProcesoComienzoDevart> ModificarProcesoComienzo(ProcesoComienzoRequest dto);
        #endregion PROCESO COMIENZO
    }
}