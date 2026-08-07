import { formatPaymentDeadline } from '../../inscriptions/models/inscription-flow-view';

export type InscripcionEstado = string;

export const PENDING_PAYMENT_STATUS = 'Pago pendiente';

export interface MiInscripcionSeminario {
  idInscripto: number;
  idOferta: number;
  descripcionOferta: string;
  idComienzo: number;
  idTurno: number;
  nombreComienzo: string;
  nombreTurno: string;
}

export interface MiInscripcion {
  idInscripto: number;
  idOfertas: number[];
  idProducto: number;
  idProceso: number;
  idComienzo: number;
  idTurno: number;
  nombreProducto: string;
  nombreComienzo: string;
  nombreTurno: string;
  estado: InscripcionEstado;
  /** Fecha de vencimiento del pago pendiente, tal como llega de la API. `null` si no se informa. */
  fechaVencimientoPago: string | null;
  seminarios: MiInscripcionSeminario[];
}

/**
 * Fecha de vencimiento lista para mostrar (`dd/MM/yyyy`) o `''` cuando no hay dato usable.
 * `formatPaymentDeadline` devuelve el centinela `'No informado'`, que aquí no queremos mostrar.
 */
export function formatFechaVencimientoPago(value: string | null | undefined): string {
  const deadline = formatPaymentDeadline(value);

  return deadline === 'No informado' ? '' : deadline;
}

export interface PendingPaymentSummary {
  title: string;
  detail: string;
  navigable: boolean;
  target: { idProducto: number; idProceso: number } | null;
}

/**
 * Resumen del alert de pago pendiente del dashboard. El título va en plural apenas hay más de
 * una inscripción pendiente, sin importar si comparten fecha. El detalle, en cambio, deduplica
 * fechas repetidas (`Set`): con una sola fecha usable la menciona una vez aunque haya varias
 * inscripciones. La navegación directa al pago sólo se habilita cuando hay una única inscripción
 * pendiente en total: con 2+ pendientes (aunque compartan fecha) no hay un destino de pago único
 * al que navegar, así que la flecha queda oculta.
 */
export function buildPendingPaymentSummary(inscripciones: MiInscripcion[]): PendingPaymentSummary {
  const pending = inscripciones.filter(
    inscripcion => inscripcion.estado === PENDING_PAYMENT_STATUS
  );

  const navigable = pending.length === 1;
  const target = navigable
    ? { idProducto: pending[0].idProducto, idProceso: pending[0].idProceso }
    : null;
  const title =
    pending.length <= 1 ? 'Inscripción pendiente de pago.' : 'Inscripciones pendientes de pago.';

  const uniqueDeadlines = [
    ...new Set(
      pending
        .map(inscripcion => formatFechaVencimientoPago(inscripcion.fechaVencimientoPago))
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
        : `Tus inscripciones pendientes de pago vencerán el ${uniqueDeadlines[0]}.`;

    return { title, detail, navigable, target };
  }

  const listed = new Intl.ListFormat('es-UY', { type: 'conjunction' }).format(uniqueDeadlines);

  return {
    title,
    detail: `Tus inscripciones pendientes de pago vencerán los días ${listed}.`,
    navigable,
    target,
  };
}
