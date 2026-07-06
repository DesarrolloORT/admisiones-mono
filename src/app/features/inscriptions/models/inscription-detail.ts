import type { InscripcionPreEnrollmentResponse } from './inscription-flow';

export interface InscripcionSummary {
  idOferta: number | null;
  idProducto: number | null;
  carrera: string | null;
  idComienzo: number | null;
  comienzo: string | null;
  idTurno: number | null;
  turno: string | null;
}

export interface InscripcionPendingPaymentDetail {
  idInscripcion: number | null;
  senia: number | null;
  saldoCuenta: number | null;
  fechaVencimientoPago: string | null;
  resumen: InscripcionSummary | null;
}

export interface InscripcionConfirmedDetail {
  numeroEstudiante: number | null;
  resumen: InscripcionSummary | null;
  coordinadorAcademico: {
    nombre: string | null;
    email: string | null;
  } | null;
  materiasPrimerSemestre: Array<{
    idMateria: number | null;
    nombre: string | null;
  }>;
}

export interface InscripcionDetail {
  estado: string | null;
  detalle: InscripcionSummary | null;
  pagoPendiente: InscripcionPendingPaymentDetail | null;
  confirmada: InscripcionConfirmedDetail | null;
}

// El flujo de pago/confirmación pinta la pantalla desde el preEnrollmentResponse del
// store. Al retomar una inscripción desde el panel reconstruimos esa misma forma a
// partir del detalle (seña, vencimiento y resumen carrera/comienzo/turno) para que
// el monto, la fecha y el resumen se muestren sin tener que rehacer la preinscripción.
export function detailToPreEnrollment(
  detail: InscripcionDetail
): InscripcionPreEnrollmentResponse | null {
  const source = detail.pagoPendiente ?? detail.confirmada;
  if (!source) return null;

  const resumen = source.resumen;
  return {
    idInscripcion: detail.pagoPendiente?.idInscripcion ?? null,
    confirmada: detail.confirmada !== null,
    fechaVencimientoPago: detail.pagoPendiente?.fechaVencimientoPago ?? null,
    seniaInscripcion: detail.pagoPendiente?.senia ?? null,
    saldoCuenta: detail.pagoPendiente?.saldoCuenta ?? null,
    resumen: resumen
      ? { carrera: resumen.carrera, comienzo: resumen.comienzo, turno: resumen.turno }
      : null,
  };
}
