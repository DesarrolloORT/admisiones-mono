using AppLogic.Dtos.Autenticacion;
using AppLogic.Personas.Requests;
using AppLogic.Personas.Responses;
using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.Personas.Interfaces
{
    public interface IPersonaService
    {
        OperationResult<DtoDatosPersona> ObtenerDatosPersona(long codigoPersona);
        OperationResult<bool> ActualizarDatosPersona(long codigoPersona, DtoActualizarDatosPersonaRequest request);
        OperationResult<bool> EsTelefonoValidoFront(DtoTelefono telefonoValidar, bool telefono1);
        OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>> ObtenerMisInscripciones(long codigoPersona);
        Task<OperationResult<object>> CambiarPasswordAsync(long codigoPersona, DtoCambiarPasswordRequest request);
        OperationResult<DtoDocumentoPersonaResponse> ObtenerDocumentoPersona(long codigoPersona);
        OperationResult<byte[]> ObtenerFotoPersona(long codigoPersona);
        OperationResult<bool> SubirFotoPersona(long codigoPersona, byte[] fileContent, string fileName);
        OperationResult<bool> SubirDocumentoPersona(long codigoPersona, DateTime fecha, DtoDocumentoPersonaArchivo frente, DtoDocumentoPersonaArchivo dorso);
    }
}
