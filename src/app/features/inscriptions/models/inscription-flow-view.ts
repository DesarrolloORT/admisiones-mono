import type { InstruccionReserva, ItemResumenInscripcion } from './inscription-flow';
import {
  InscripcionPreEnrollmentResponse,
  MetodoPago,
  OpcionInscripcion,
} from './inscription-flow';
import { getOptionLabel } from './inscription-flow-options';
import { RESERVATION_INSTRUCTIONS } from './inscription-static-data';

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
  if (typeof value !== 'number' || !Number.isFinite(value) || value < 0) return 'No informado';

  return `$ ${new Intl.NumberFormat('es-UY', { maximumFractionDigits: 0 }).format(value)}`;
}

export function formatPaymentDeadline(value: string | null | undefined): string {
  if (!value) return 'No informado';
  if (/^\d{2}\/\d{2}\/\d{4}$/.test(value)) return value;

  const isoDate = /^(\d{4})-(\d{2})-(\d{2})(?:T.*)?$/.exec(value);
  if (!isoDate) return 'No informado';

  const [, year, month, day] = isoDate;
  const date = new Date(`${year}-${month}-${day}T00:00:00Z`);
  if (
    Number.isNaN(date.getTime()) ||
    date.getUTCFullYear() !== Number(year) ||
    date.getUTCMonth() + 1 !== Number(month) ||
    date.getUTCDate() !== Number(day)
  ) {
    return 'No informado';
  }

  return `${day}/${month}/${year}`;
}

export function getReservationInstructions(method: MetodoPago | null): InstruccionReserva {
  return method === 'paganza' || method === 'banred' || method === 'abitab'
    ? RESERVATION_INSTRUCTIONS[method]
    : RESERVATION_INSTRUCTIONS.abitab;
}
