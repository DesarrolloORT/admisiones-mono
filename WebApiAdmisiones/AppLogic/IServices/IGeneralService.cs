using Utilities;

namespace AppLogic.IServices
{
    public interface IGeneralService
    {
        OperationResult<DateTime> CalcularFechaVencimientoAdmisiones(long codigoPersona, long idProceso);
    }
}
