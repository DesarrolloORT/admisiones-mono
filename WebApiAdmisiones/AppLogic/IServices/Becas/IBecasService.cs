using AppLogic.DevartDTOs;
using Utilities;

namespace AppLogic.IServices.Becas
{
    public interface IBecasService
    {
        OperationResult<DtoAceptacionReglamentoEstDevart> ObtenerAceptacionReglamentoEstudiantil(long codigoPersona);
        OperationResult<IEnumerable<DtoPruebaDevart>> ObtenerFondosDeBecaVigentes(long idProducto, long idProceso, long codigoPersona);
        OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>> ObtenerMisInscripcionesConfirmadas(long codigoPersona);
    }
}
