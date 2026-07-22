using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.Becas.Interfaces;

public interface IBecasService
{
    OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>> ObtenerMisInscripcionesConfirmadas(long codigoPersona);
}
