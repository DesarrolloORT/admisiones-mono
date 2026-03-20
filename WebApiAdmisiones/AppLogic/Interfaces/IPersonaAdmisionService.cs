using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.Interfaces
{
    public interface IPersonaAdmisionService
    {
        OperationResult<DtoPersonaDevart> ObtenerPersona(long codigoPersona);
        OperationResult<DtoEncuestaIniAdmisionDevart> ObtenerEncuestaInicialAdmision(long codigoPersona);
        OperationResult<byte[]> ObtenerDocumentoAlumno(long codigoPersona, int tipo);
        OperationResult<byte[]> ObtenerFotoAlumno(long codigoPersona);
        OperationResult<bool> SubirFotoAlumno(long codigoPersona, byte[] fileContent, string fileName);
        OperationResult<bool> SubirDocumentoAlumno(long codigoPersona, int tipo, DateTime fecha, byte[] fileContent, string fileName);
    }
}
