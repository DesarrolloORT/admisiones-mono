using AppLogic.DTOs;
using AppLogic.Requests;
using Utilities;

namespace AppLogic.IServices
{
    public interface IPersonaService
    {
        OperationResult<DtoDatosPersona> ObtenerDatosPersona(long codigoPersona);
        OperationResult<bool> ActualizarDatosPersona(long codigoPersona, ActualizarDatosPersonaRequest request);
        Task<OperationResult<object>> CambiarPasswordAsync(long codigoPersona, DtoCambiarPasswordRequest request);
    }
}
