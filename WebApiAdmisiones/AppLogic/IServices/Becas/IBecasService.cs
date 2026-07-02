using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.IServices.Becas
{
    public interface IBecasService
    {
        OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>> ObtenerMisInscripcionesConfirmadas(long codigoPersona);
    }
}
