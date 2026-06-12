using AppLogic.DevartDTOs;
using AppLogic.Requests;
using Utilities;

namespace AppLogic.IServices.Personas
{
    public interface IPersonaAdmisionService
    {
        OperationResult<bool> GuardarDatosPersonaEncuesta(long codigoPersona, GuardarDatosPersonaEncuestaRequest request);
    }
}
