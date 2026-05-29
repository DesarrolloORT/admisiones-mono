using AppLogic.DevartDTOs;
using AppLogic.Requests;
using Utilities;

namespace AppLogic.IServices
{
    public interface IPersonaAdmisionService
    {
        OperationResult<DtoEncuestaIniAdmisionDevart> ObtenerEncuestaInicialAdmision(long codigoPersona);
        OperationResult<bool> GuardarDatosPersonaEncuesta(long codigoPersona, GuardarDatosPersonaEncuestaRequest request);
    }
}
