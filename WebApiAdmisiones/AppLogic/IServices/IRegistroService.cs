using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices
{
    public interface IRegistroService
    {
        OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaises();
        OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>> ObtenerTipoDocumentos();
        OperationResult<IEnumerable<DtoProcesoDevart>> ObtenerProcesosHabilitadosPorProducto(long idProducto);
        OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaisesEstadosCiudades();
        OperationResult<IEnumerable<DtoProductoAdmisiones>> ObtenerProductosVigentes();
    }
}
