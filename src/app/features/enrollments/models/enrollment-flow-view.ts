import type {
  EnrollmentReservationData,
  EnrollmentSummaryItem,
  ReservationInstructionItem,
  ReservationInstructions,
  SeminarSummaryItem,
} from './enrollment-flow';
import {
  EnrollmentOption,
  EnrollmentPreEnrollmentResponse,
  PaymentMethod,
} from './enrollment-flow';
import { getOptionLabel } from './enrollment-flow-options';

export function buildSummaryItems(context: {
  response: EnrollmentPreEnrollmentResponse | null;
  selectedDegreeProgram: string;
  selectedIntake: string;
  selectedShift: string;
  degreeProgramOptions: readonly EnrollmentOption[];
  intakeOptions: readonly EnrollmentOption[];
  shiftOptions: readonly EnrollmentOption[];
  isProfessionalUpdate: boolean;
  seminars: readonly SeminarSummaryItem[];
}): EnrollmentSummaryItem[] {
  const degreeProgramValue =
    context.response?.summary?.degreeProgram ??
    getOptionLabel(context.degreeProgramOptions, context.selectedDegreeProgram, 'Sin seleccionar');

  if (context.isProfessionalUpdate) {
    const program = { icon: 'school', label: 'Programa', value: degreeProgramValue };
    // Un solo seminario: se lee como los niveles 1/2, con su comienzo como fila del
    // resumen en lugar del bloque "Seminarios" (mismo criterio que DashboardCard).
    if (context.seminars.length !== 1) return [program];

    return [
      program,
      { icon: 'calendar_today', label: 'Comienzo', value: context.seminars[0].intake },
    ];
  }

  return [
    { icon: 'school', label: 'Carrera', value: degreeProgramValue },
    {
      icon: 'calendar_today',
      label: 'Comienzo',
      value:
        context.response?.summary?.intake ??
        getOptionLabel(context.intakeOptions, context.selectedIntake, 'Sin seleccionar'),
    },
    {
      icon: 'schedule',
      label: 'Turno',
      value:
        context.response?.summary?.shift ??
        getOptionLabel(context.shiftOptions, context.selectedShift, 'Sin seleccionar'),
    },
  ];
}

// Actualización profesional: una fila por seminario, fuera de summaryItems para no
// romper el `.slice(1)` que usa la pantalla de éxito.
export function buildSeminarsSummary(
  response: EnrollmentPreEnrollmentResponse | null
): SeminarSummaryItem[] {
  return (response?.seminars ?? []).map(seminar => ({
    enrollmentId: seminar.enrollmentId,
    name: seminar.name ?? 'No informado',
    intake: seminar.intake ?? 'No informado',
    shift: seminar.shift ?? 'No informado',
  }));
}

export function formatEnrollmentAmount(value: number | null | undefined): string {
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
  method: PaymentMethod | null,
  response: EnrollmentPreEnrollmentResponse | null,
  reservation: EnrollmentReservationData | null
): ReservationInstructions {
  // Seña 0: no hay nada que cobrar, así que no corresponde pedir medio de pago ni
  // mostrar fecha límite/monto/cédula. El proceso queda en manos de la oficina.
  if (response?.enrollmentDeposit === 0) {
    return {
      title: '¡Inscripción reservada!',
      description: ZERO_DEPOSIT_CONTACT_MESSAGE,
      intro: '',
      items: [],
      help: '',
    };
  }

  const deadline = formatPaymentDeadline(response?.paymentDueDate);
  const description =
    deadline === 'No informado'
      ? 'Realizá el pago de la seña para mantener tu lugar. Si no se acredita, la inscripción se dará de baja automáticamente.'
      : `Tenés tiempo hasta el ${deadline} para realizar el pago de la seña. Pasada esa fecha, la inscripción se dará de baja automáticamente.`;

  const documentNumber = reservation?.documentNumber;
  const studentNumber =
    typeof reservation?.personCode === 'number' ? String(reservation.personCode) : null;
  const amount = formatEnrollmentAmount(response?.enrollmentDeposit);

  const documentNumberItem: ReservationInstructionItem[] = documentNumber
    ? [{ label: 'Cédula de identidad', value: documentNumber }]
    : [];
  const studentItem: ReservationInstructionItem[] = studentNumber
    ? [{ label: 'Número de estudiante', value: studentNumber }]
    : [];
  const amountItem: ReservationInstructionItem[] = amount
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
    items: [...documentNumberItem, ...studentItem, ...amountItem],
    help: RESERVATION_HELP,
  };
}
