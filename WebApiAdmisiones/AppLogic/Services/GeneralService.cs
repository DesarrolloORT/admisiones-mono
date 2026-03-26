using AppLogic.IServices;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Services
{
    public class GeneralService : IGeneralService
    {
        private readonly IUnitOfWorkFactory _uowFactory;

        public GeneralService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        public OperationResult<DateTime> CalcularFechaVencimientoAdmisiones(long codigoPersona, long idProceso)
        {
            using var uow = _uowFactory.Create();

            var proceso = uow.Procesos.GetByKey(idProceso);
            if (proceso?.ComienzoSemestre1Proceso == null)
            {
                return OperationResult<DateTime>.IsFailed(
                    "GEN_FVA_01",
                    nameof(CalcularFechaVencimientoAdmisiones),
                    "Problema con la carga de fecha del comienzo del proceso.",
                    400);
            }

            var fechaComienzoSemestre = proceso.ComienzoSemestre1Proceso.Value;
            var fechaActual = DateTime.Now;
            const int cantDiasHabiles = 5;

            DateTime fechaVencimiento;
            if (fechaActual >= fechaComienzoSemestre)
            {
                fechaVencimiento = AddDiasHabilesaFecha(uow, fechaActual, 1, false);
            }
            else if (fechaActual > AddDiasHabilesaFecha(uow, fechaComienzoSemestre, cantDiasHabiles, true))
            {
                fechaVencimiento = fechaComienzoSemestre;
            }
            else
            {
                fechaVencimiento = AddDiasHabilesaFecha(uow, fechaActual, cantDiasHabiles, false);
            }

            var declaracion = uow.DeclaracionJuradaWebs.GetFechaEntregaDjAdmisiones(codigoPersona);
            if (declaracion.HasValue && declaracion.Value < fechaVencimiento)
            {
                fechaVencimiento = declaracion.Value;
            }

            return OperationResult<DateTime>.Ok(fechaVencimiento, nameof(CalcularFechaVencimientoAdmisiones));
        }

        private static DateTime AddDiasHabilesaFecha(IUnitOfWork uow, DateTime fecha, int cantDias, bool restar)
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
}
