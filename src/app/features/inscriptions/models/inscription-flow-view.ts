import type { InstruccionReserva, ItemResumenInscripcion } from './inscription-flow';
import {
  InscripcionPreEnrollmentResponse,
  MetodoPago,
  OpcionInscripcion,
} from './inscription-flow';
import { getOptionLabel } from './inscription-flow-options';

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

// Las instrucciones se arman con la fecha y el monto reales de la preinscripción.
// El número de estudiante no está disponible en esta etapa (recién llega con la
// inscripción confirmada), por eso las instrucciones no lo incluyen.
export function buildReservationInstructions(
  method: MetodoPago | null,
  response: InscripcionPreEnrollmentResponse | null
): InstruccionReserva {
  const deadline = formatPaymentDeadline(response?.fechaVencimientoPago);
  const amount = formatInscriptionAmount(response?.seniaInscripcion);
  const description =
    deadline === 'No informado'
      ? 'Realizá el pago de la seña para mantener tu lugar. Si no se acredita, la inscripción se dará de baja automáticamente.'
      : `Tenés tiempo hasta el ${deadline} para realizar el pago de la seña. Pasada esa fecha, la inscripción se dará de baja automáticamente.`;
  const amountItems = amount === 'No informado' ? [] : [`Monto a pagar: ${amount}`];

  if (method === 'paganza') {
    return {
      title: '¡Inscripción reservada!',
      description,
      items: ['Buscá Universidad ORT Uruguay en la aplicación', ...amountItems],
      help: 'La acreditación puede demorar hasta 24 horas hábiles.',
    };
  }

  if (method === 'abitab') {
    return {
      title: '¡Inscripción reservada!',
      description,
      items: ['Cédula de identidad: documento registrado', ...amountItems],
      help: 'El pago puede demorar hasta 24 horas hábiles en acreditarse en el sistema.',
    };
  }

  return {
    title: '¡Inscripción reservada!',
    description,
    items: amountItems,
    help: 'El pago puede demorar hasta 24 horas hábiles en acreditarse en el sistema.',
  };
}
