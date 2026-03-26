using AppLogic.DevartDTOs;
using AppLogic.Requests;
using Utilities;

namespace AppLogic.IServices
{
    public interface IPersonaAdmisionService
    {
        OperationResult<DtoPersonaDevart> ObtenerPersona(long codigoPersona);
        OperationResult<bool> ActualizarPersona(long codigoPersona, ActualizarPersonaRequest request);
        OperationResult<DtoEncuestaIniAdmisionDevart> ObtenerEncuestaInicialAdmision(long codigoPersona);
        OperationResult<bool> GuardarDatosPersonaEncuesta(long codigoPersona, GuardarDatosPersonaEncuestaRequest request);
        OperationResult<byte[]> ObtenerDocumentoAlumno(long codigoPersona, int tipo);
        OperationResult<byte[]> ObtenerFotoAlumno(long codigoPersona);
        OperationResult<bool> SubirFotoAlumno(long codigoPersona, byte[] fileContent, string fileName);
        OperationResult<bool> SubirDocumentoAlumno(long codigoPersona, int tipo, DateTime fecha, byte[] fileContent, string fileName);
    }
}
