import type {
  InscripcionReservationData,
  InstruccionReserva,
  ItemInstruccionReserva,
  ItemResumenInscripcion,
  ItemSeminarioResumen,
} from './inscription-flow';
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
  isProfessionalUpdate: boolean;
}): ItemResumenInscripcion[] {
  const carreraValue =
    context.response?.resumen?.carrera ??
    getOptionLabel(context.careerOptions, context.selectedCareer, 'Sin seleccionar');

  if (context.isProfessionalUpdate) {
    return [{ icon: 'school', label: 'Programa', value: carreraValue }];
  }

  return [
    { icon: 'school', label: 'Carrera', value: carreraValue },
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

// Actualización profesional: una fila por seminario, fuera de summaryItems para no
// romper el `.slice(1)` que usa la pantalla de éxito.
export function buildSeminariosSummary(
  response: InscripcionPreEnrollmentResponse | null
): ItemSeminarioResumen[] {
  return (response?.seminarios ?? []).map(seminario => ({
    idInscripcion: seminario.idInscripcion,
    nombre: seminario.nombre ?? 'No informado',
    comienzo: seminario.comienzo ?? 'No informado',
    turno: seminario.turno ?? 'No informado',
  }));
}

export function formatInscriptionAmount(value: number | null | undefined): string {
  if (typeof value !== 'number' || !Number.isFinite(value) || value < 0) return '';

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

const RESERVATION_HELP =
  'El pago puede demorar hasta 24 horas hábiles en acreditarse en el sistema.';

const ZERO_DEPOSIT_CONTACT_MESSAGE =
  'Para continuar el proceso de inscripciones debe comunicarse con la oficina de Admisiones.';

// Las instrucciones se arman con la fecha, el monto, la cédula y el número de
// estudiante (código de persona) reales que informa el backend. Los ítems sin
// dato real no se muestran (nunca se inventa un placeholder).
export function buildReservationInstructions(
  method: MetodoPago | null,
  response: InscripcionPreEnrollmentResponse | null,
  reservation: InscripcionReservationData | null
): InstruccionReserva {
  // Seña 0: no hay nada que cobrar, así que no corresponde pedir medio de pago ni
  // mostrar fecha límite/monto/cédula. El proceso queda en manos de la oficina.
  if (response?.seniaInscripcion === 0) {
    return {
      title: '¡Inscripción reservada!',
      description: ZERO_DEPOSIT_CONTACT_MESSAGE,
      intro: '',
      items: [],
      help: '',
    };
  }

  const deadline = formatPaymentDeadline(response?.fechaVencimientoPago);
  const description =
    deadline === 'No informado'
      ? 'Realizá el pago de la seña para mantener tu lugar. Si no se acredita, la inscripción se dará de baja automáticamente.'
      : `Tenés tiempo hasta el ${deadline} para realizar el pago de la seña. Pasada esa fecha, la inscripción se dará de baja automáticamente.`;

  const cedula = reservation?.cedula;
  const studentNumber =
    typeof reservation?.codigoPersona === 'number' ? String(reservation.codigoPersona) : null;
  const amount = formatInscriptionAmount(response?.seniaInscripcion);

  const cedulaItem: ItemInstruccionReserva[] = cedula
    ? [{ label: 'Cédula de identidad', value: cedula }]
    : [];
  const studentItem: ItemInstruccionReserva[] = studentNumber
    ? [{ label: 'Número de estudiante', value: studentNumber }]
    : [];
  const amountItem: ItemInstruccionReserva[] = amount
    ? [{ label: 'Monto a pagar', value: amount }]
    : [];

  if (method === 'paganza') {
    return {
      title: '¡Inscripción reservada!',
      description,
      intro:
        'Ingresá a Paganza y realizá un nuevo pago a Universidad ORT Uruguay ingresando la siguiente información:',
      items: [...studentItem, ...amountItem],
      help: RESERVATION_HELP,
    };
  }

  return {
    title: '¡Inscripción reservada!',
    description,
    intro: 'Dirigite a cualquier local habilitado presentando la siguiente información:',
    items: [...cedulaItem, ...studentItem, ...amountItem],
    help: RESERVATION_HELP,
  };
}
