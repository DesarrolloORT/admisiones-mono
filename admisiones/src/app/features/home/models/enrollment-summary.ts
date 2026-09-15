import { formatPaymentDeadline } from '../../enrollments/models/enrollment-flow-view';

export type EnrollmentStatus = string;

export const PENDING_PAYMENT_STATUS = 'Pago pendiente';

export interface EnrollmentSeminarSummary {
  enrollmentId: number;
  offeringId: number;
  offeringDescription: string;
  intakeId: number;
  shiftId: number;
  intakeName: string;
  shiftName: string;
}

export interface EnrollmentSummary {
  enrollmentId: number;
  offeringIds: number[];
  productId: number;
  admissionProcessId: number;
  productLevelId: number | null;
  intakeId: number;
  shiftId: number;
  degreeProgramName: string;
  intakeName: string;
  shiftName: string;
  status: EnrollmentStatus;
  /** Fecha de vencimiento del pago pendiente, tal como llega de la API. `null` si no se informa. */
  paymentDueDate: string | null;
  seminars: EnrollmentSeminarSummary[];
}

/**
 * Fecha de vencimiento lista para mostrar (`dd/MM/yyyy`) o `''` cuando no hay dato usable.
 * `formatPaymentDeadline` devuelve el centinela `'No informado'`, que aquí no queremos mostrar.
 */
export function formatPaymentDueDate(value: string | null | undefined): string {
  const deadline = formatPaymentDeadline(value);

  return deadline === 'No informado' ? '' : deadline;
}

export interface PendingPaymentSummary {
  title: string;
  detail: string;
  navigable: boolean;
  target: { idProducto: number; idProceso: number; estado: string } | null;
}

/**
 * Resumen del alert de pago pendiente del dashboard. El título va en plural apenas hay más de
 * una inscripción pendiente, sin importar si comparten fecha. El detalle, en cambio, deduplica
 * fechas repetidas (`Set`): con una sola fecha usable la menciona una vez aunque haya varias
 * inscripciones. La navegación directa al pago sólo se habilita cuando hay una única inscripción
 * pendiente en total: con 2+ pendientes (aunque compartan fecha) no hay un destino de pago único
 * al que navegar, así que la flecha queda oculta.
 */
export function buildPendingPaymentSummary(
  enrollments: EnrollmentSummary[]
): PendingPaymentSummary {
  const pending = enrollments.filter(enrollment => enrollment.status === PENDING_PAYMENT_STATUS);

  const navigable = pending.length === 1;
  const target = navigable
    ? {
        idProducto: pending[0].productId,
        idProceso: pending[0].admissionProcessId,
        estado: pending[0].status,
      }
    : null;
  const title =
    pending.length <= 1 ? 'Inscripción pendiente de pago.' : 'Inscripciones pendientes de pago.';

  const uniqueDeadlines = [
    ...new Set(
      pending
        .map(enrollment => formatPaymentDueDate(enrollment.paymentDueDate))
        .filter(deadline => deadline !== '')
    ),
  ];

  if (uniqueDeadlines.length === 0) {
    return { title, detail: 'Consultá el detalle desde Mis carreras.', navigable, target };
  }

  if (uniqueDeadlines.length === 1) {
    const detail =
      pending.length === 1
        ? `Realizá el pago antes del ${uniqueDeadlines[0]}.`
        : `Las mismas vencerán el ${uniqueDeadlines[0]}.`;

    return { title, detail, navigable, target };
  }

  const listed = new Intl.ListFormat('es-UY', { type: 'conjunction' }).format(uniqueDeadlines);

  return {
    title,
    detail: `Las mismas vencerán los días ${listed}.`,
    navigable,
    target,
  };
}
