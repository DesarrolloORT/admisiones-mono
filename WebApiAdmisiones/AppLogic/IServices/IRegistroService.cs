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
        Task<OperationResult<object?>> ConfirmarNuevaPersonaAsync(RegistroPersonaRequest request);
        Task<OperationResult<object?>> ConfirmarSolicitudAltaAsync(RegistroPersonaRequest request);
    }
}
