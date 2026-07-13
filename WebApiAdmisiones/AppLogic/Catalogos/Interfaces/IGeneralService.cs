using Utilities;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.Catalogos.Interfaces
{
    public interface IGeneralService
    {
        OperationResult<DateTime> CalcularFechaVencimientoAdmisiones(IUnitOfWork uow, long codigoPersona, long idProceso);
    }
}
