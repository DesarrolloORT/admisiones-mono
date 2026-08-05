using AppLogic.Enrollments.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.Services;

public class AdmissionDueDateCalculator(IUnitOfWorkFactory uowFactory) : IAdmissionDueDateCalculator
{
    private const int DueDateBusinessDays = 5;
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;

    public OperationResult<DateTime> CalculateAdmissionDueDate(IUnitOfWork uow, long personId, long admissionProcessId)
    {
        var admissionProcess = uow.Procesos.GetByKey(admissionProcessId);
        if (admissionProcess?.ComienzoSemestre1Proceso == null)
        {
            return OperationResult<DateTime>.IsFailed(
                "GEN_FVA_01",
                nameof(CalculateAdmissionDueDate),
                "Problema con la carga de fecha del comienzo del proceso.",
                400);
        }

        var semesterStartDate = admissionProcess.ComienzoSemestre1Proceso.Value;
        var currentDate = DateTime.Now;
        DateTime dueDate;
        if (currentDate >= semesterStartDate)
        {
            dueDate = AddBusinessDays(uow, currentDate, 1, false);
        }
        else if (currentDate > AddBusinessDays(uow, semesterStartDate, DueDateBusinessDays, true))
        {
            dueDate = semesterStartDate;
        }
        else
        {
            dueDate = AddBusinessDays(uow, currentDate, DueDateBusinessDays, false);
        }

        var affidavit = uow.DeclaracionJuradaWebs.GetFechaEntregaDjAdmisiones(personId);
        if (affidavit.HasValue && affidavit.Value < dueDate)
        {
            dueDate = affidavit.Value;
        }

        return OperationResult<DateTime>.Ok(dueDate, nameof(CalculateAdmissionDueDate));
    }

    /// <summary>
    /// Avanza (o retrocede) una fecha una cantidad de días hábiles, salteando sábados, domingos y feriados.
    /// </summary>
    /// <param name="uow">Unidad de trabajo activa para consultar feriados.</param>
    /// <param name="fecha">Fecha de partida.</param>
    /// <param name="cantDias">Cantidad de días hábiles a contar.</param>
    /// <param name="restar">Si es <c>true</c> retrocede en el calendario; si es <c>false</c> avanza.</param>
    /// <returns>Fecha resultante tras contar los días hábiles indicados.</returns>
    private static DateTime AddBusinessDays(IUnitOfWork uow, DateTime fecha, int cantDias, bool restar)
    {
        int signo = restar ? -1 : 1;
        int diasContados = 0;
        while (diasContados < cantDias)
        {
            fecha = fecha.AddDays(signo);
            if (fecha.DayOfWeek != DayOfWeek.Saturday
                && fecha.DayOfWeek != DayOfWeek.Sunday
                && !uow.Feriados.EsFeriado(fecha))
            {
                diasContados++;
            }
        }

        return fecha;
    }
}
