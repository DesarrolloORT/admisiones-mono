using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices
{
    public interface IRegistroService
    {
        Task<OperationResult<RegistroEvaluacionResponse>> EvaluarDocumentoAsync(RegistroEvaluarDocumentoRequest request);
        Task<OperationResult<object?>> ConfirmarRegistroAsync(RegistroConfirmarRequest request);
        OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>> ObtenerTipoDocumentos();
        OperationResult<IEnumerable<RegistroComienzoResponse>> ObtenerComienzos(long idCarrera);
        OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaisesEstadosCiudades();
        OperationResult<IEnumerable<RegistroCarreraResponse>> ObtenerCarreras();
    }
}
