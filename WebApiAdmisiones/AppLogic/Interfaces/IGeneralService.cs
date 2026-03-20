using Utilities;

namespace AppLogic.Interfaces
{
    public interface IGeneralService
    {
        OperationResult<DateTime> CalcularFechaVencimientoAdmisiones(long codigoPersona, long idProceso);
    }
}
