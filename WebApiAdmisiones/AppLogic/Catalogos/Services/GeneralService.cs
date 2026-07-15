using AppLogic.Catalogos.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Catalogos.Services
{
    public class GeneralService : IGeneralService
    {
        private const int CantidadDiasHabilesVencimiento = 5;
        private readonly IUnitOfWorkFactory _uowFactory;

        public GeneralService(IUnitOfWorkFactory uowFactory)
        {
            _uowFactory = uowFactory;
        }

        public OperationResult<DateTime> CalcularFechaVencimientoAdmisiones(IUnitOfWork uow, long codigoPersona, long idProceso)
        {
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
            DateTime fechaVencimiento;
            if (fechaActual >= fechaComienzoSemestre)
            {
                fechaVencimiento = AgregarDiasHabilesAFecha(uow, fechaActual, 1, false);
            }
            else if (fechaActual > AgregarDiasHabilesAFecha(uow, fechaComienzoSemestre, CantidadDiasHabilesVencimiento, true))
            {
                fechaVencimiento = fechaComienzoSemestre;
            }
            else
            {
                fechaVencimiento = AgregarDiasHabilesAFecha(uow, fechaActual, CantidadDiasHabilesVencimiento, false);
            }

            var declaracion = uow.DeclaracionJuradaWebs.GetFechaEntregaDjAdmisiones(codigoPersona);
            if (declaracion.HasValue && declaracion.Value < fechaVencimiento)
            {
                fechaVencimiento = declaracion.Value;
            }

            return OperationResult<DateTime>.Ok(fechaVencimiento, nameof(CalcularFechaVencimientoAdmisiones));
        }

        /// <summary>
        /// Avanza (o retrocede) una fecha una cantidad de días hábiles, salteando sábados, domingos y feriados.
        /// </summary>
        /// <param name="uow">Unidad de trabajo activa para consultar feriados.</param>
        /// <param name="fecha">Fecha de partida.</param>
        /// <param name="cantDias">Cantidad de días hábiles a contar.</param>
        /// <param name="restar">Si es <c>true</c> retrocede en el calendario; si es <c>false</c> avanza.</param>
        /// <returns>Fecha resultante tras contar los días hábiles indicados.</returns>
        private static DateTime AgregarDiasHabilesAFecha(IUnitOfWork uow, DateTime fecha, int cantDias, bool restar)
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
