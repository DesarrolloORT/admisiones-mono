using Utilities;
using BusinessLogic.IDevartRepositories;

namespace AppLogic.Catalogos.Interfaces;

public interface IGeneralService
{
    /// <summary>
    /// Calcula la fecha de vencimiento para la admisión de una persona en un proceso, en base al comienzo del
    /// semestre y a los días hábiles configurados, acotada por la fecha de entrega de la declaración jurada si existe.
    /// </summary>
    /// <param name="uow">Unidad de trabajo activa sobre la que se resuelven las consultas.</param>
    /// <param name="codigoPersona">Código de la persona.</param>
    /// <param name="idProceso">Identificador del proceso.</param>
    /// <returns>Fecha de vencimiento calculada.</returns>
    OperationResult<DateTime> CalcularFechaVencimientoAdmisiones(IUnitOfWork uow, long codigoPersona, long idProceso);
}
