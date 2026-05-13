using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using Utilities;

namespace AppLogic.IServices
{
    public interface IRegistroService
    {
        Task<OperationResult<RegistroEvaluacionResponse>> EvaluarDocumentoAsync(RegistroEvaluarDocumentoRequest request);
        Task<OperationResult<object?>> VerificarIdentidadAsync(RegistroVerificarIdentidadRequest request);
        Task<OperationResult<object?>> ConfirmarPersonaExistenteAsync(RegistroConfirmarPersonaExistenteRequest request);
        Task<OperationResult<object?>> ConfirmarNuevaPersonaAsync(RegistroConfirmarNuevaPersonaRequest request);
        Task<OperationResult<object?>> ConfirmarSolicitudAltaAsync(RegistroConfirmarSolicitudAltaRequest request);
        OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>> ObtenerTipoDocumentos();
        OperationResult<IEnumerable<RegistroComienzoResponse>> ObtenerComienzos(long idCarrera);
        OperationResult<IEnumerable<DtoPaisDevart>> ObtenerPaisesEstadosCiudades();
        OperationResult<IEnumerable<RegistroCarreraResponse>> ObtenerCarreras();
    }
}
