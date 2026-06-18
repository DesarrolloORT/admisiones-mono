using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Requests;
using Utilities;

namespace AppLogic.IServices.Personas
{
    public interface IPersonaService
    {
        OperationResult<DtoDatosPersona> ObtenerDatosPersona(long codigoPersona);
        OperationResult<bool> ActualizarDatosPersona(long codigoPersona, ActualizarDatosPersonaRequest request);
        OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>> ObtenerMisInscripciones(long codigoPersona);
        Task<OperationResult<object>> CambiarPasswordAsync(long codigoPersona, DtoCambiarPasswordRequest request);
        OperationResult<byte[]> ObtenerDocumentoPersona(long codigoPersona, int tipo);
        OperationResult<byte[]> ObtenerFotoPersona(long codigoPersona);
        OperationResult<bool> SubirFotoPersona(long codigoPersona, byte[] fileContent, string fileName);
        OperationResult<bool> SubirDocumentoPersona(long codigoPersona, int tipo, DateTime fecha, byte[] fileContent, string fileName);
    }
}
