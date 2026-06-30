using Utilities;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.IServices.Catalogos
{
    public interface IGeneralService
    {
        OperationResult<DateTime> CalcularFechaVencimientoAdmisiones(long codigoPersona, long idProceso);
        OperationResult<DateTime> CalcularFechaVencimientoAdmisiones(IUnitOfWork uow, long codigoPersona, long idProceso);
    }
}
