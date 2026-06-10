using Utilities;

namespace AppLogic.IServices.Catalogos
{
    public interface IGeneralService
    {
        OperationResult<DateTime> CalcularFechaVencimientoAdmisiones(long codigoPersona, long idProceso);
    }
}
