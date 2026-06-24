import type { InstruccionReserva, ItemResumenInscripcion } from './inscripcion-flow';
import {
  InscripcionPreEnrollmentResponse,
  MetodoPago,
  OpcionInscripcion,
} from './inscripcion-flow';
import { getOptionLabel } from './inscripcion-flow-options';
import { RESERVATION_INSTRUCTIONS } from './inscripcion-static-data';

export function buildSummaryItems(context: {
  response: InscripcionPreEnrollmentResponse | null;
  selectedCareer: string;
  selectedStart: string;
  selectedTurno: string;
  careerOptions: readonly OpcionInscripcion[];
  startOptions: readonly OpcionInscripcion[];
  turnoOptions: readonly OpcionInscripcion[];
}): ItemResumenInscripcion[] {
  return [
    {
      icon: 'school',
      label: 'Carrera',
      value:
        context.response?.resumen?.carrera ??
        getOptionLabel(context.careerOptions, context.selectedCareer, 'Sin seleccionar'),
    },
    {
      icon: 'calendar_today',
      label: 'Comienzo',
      value:
        context.response?.resumen?.comienzo ??
        getOptionLabel(context.startOptions, context.selectedStart, 'Sin seleccionar'),
    },
    {
      icon: 'schedule',
      label: 'Turno',
      value:
        context.response?.resumen?.turno ??
        getOptionLabel(context.turnoOptions, context.selectedTurno, 'Sin seleccionar'),
    },
  ];
}

export function formatInscriptionAmount(value: number | null | undefined): string {
  if (typeof value !== 'number' || !Number.isFinite(value)) return '$ 15.500';

  return `$ ${new Intl.NumberFormat('es-UY', { maximumFractionDigits: 0 }).format(value)}`;
}

export function formatPaymentDeadline(value: string | null | undefined): string {
  if (!value) return '04/03/2027';
  if (/^\d{2}\/\d{2}\/\d{4}$/.test(value)) return value;

  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  if (!year || !month || !day) return value;

  return `${day.toString().padStart(2, '0')}/${month.toString().padStart(2, '0')}/${year}`;
}

export function getReservationInstructions(method: MetodoPago | null): InstruccionReserva {
  return method === 'paganza' || method === 'banred' || method === 'abitab'
    ? RESERVATION_INSTRUCTIONS[method]
    : RESERVATION_INSTRUCTIONS.abitab;
}
