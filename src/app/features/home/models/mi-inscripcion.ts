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

/**
 * Detalle del alert de pago pendiente del dashboard. Con una sola inscripción pendiente informa su
 * fecha; con varias las lista todas (cada tarjeta repite la suya). Sin fechas usables cae al texto
 * genérico, que es lo que se muestra mientras la API no informe `fechaVencimientoPago`.
 */
export function buildPendingPaymentDetail(inscripciones: MiInscripcion[]): string {
  const deadlines = inscripciones
    .filter(inscripcion => inscripcion.estado === PENDING_PAYMENT_STATUS)
    .map(inscripcion => formatFechaVencimientoPago(inscripcion.fechaVencimientoPago))
    .filter(deadline => deadline !== '');

  if (deadlines.length === 0) {
    return 'Consultá el detalle desde Mis carreras.';
  }

  if (deadlines.length === 1) {
    return `Realizá el pago antes del ${deadlines[0]}.`;
  }

  const listed = new Intl.ListFormat('es-UY', { type: 'conjunction' }).format(deadlines);

  return `Tus inscripciones pendientes de pago vencerán los días ${listed}.`;
}
