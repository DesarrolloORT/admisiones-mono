import type {
  InscripcionOfertaResumen,
  InscripcionPreEnrollmentResponse,
} from './inscription-flow';

// La cabecera del Detalle (`DtoCabeceraInscripcion`) solo trae producto y carrera; el
// comienzo y el turno llegan como texto en la oferta asociada. La API NO expone
// `idComienzo` ni `idTurno` en ningún bloque del Detalle: no los declaramos para que el
// contrato no prometa datos que nunca llegan (fue la causa del bug de retomar AP).
export interface InscripcionSummary {
  idOferta: number | null;
  idProducto: number | null;
  carrera: string | null;
  comienzo: string | null;
  turno: string | null;
}

export interface InscripcionPendingPaymentDetail {
  idInscripcion: number | null;
  senia: number | null;
  saldoCuenta: number | null;
  fechaVencimientoPago: string | null;
  resumen: InscripcionSummary | null;
  seminarios: InscripcionOfertaResumen[];
}

export interface InscripcionCoordinador {
  nombre: string | null;
  email: string | null;
}

export interface InscripcionMinimumDeposit {
  metodoPago: string | null;
  cedula: string | null;
  codigoPersona: number | null;
  senia: number | null;
}

export interface InscripcionMateria {
  idMateria: number | null;
  nombre: string | null;
}

// Una por cada oferta confirmada, con su propio comienzo/turno/materias: en
// Actualización profesional (niveles 3 y 4) vienen varias, una por seminario.
export interface InscripcionConfirmedInscripcion {
  idInscripcion: number | null;
  idOferta: number | null;
  comienzo: string | null;
  turno: string | null;
  materiasPrimerSemestre: InscripcionMateria[];
}

// `confirmada` es una cabecera compartida (producto, carrera y coordinación) más el
// detalle por oferta confirmada; el comienzo/turno del resumen sale de la primera.
export interface InscripcionConfirmedDetail {
  numeroEstudiante: number | null;
  resumen: InscripcionSummary | null;
  coordinadorAcademico: InscripcionCoordinador | null;
  coordinadorCursos: InscripcionCoordinador | null;
  inscripciones: InscripcionConfirmedInscripcion[];
}

export interface InscripcionDetail {
  estado: string | null;
  detalle: InscripcionSummary | null;
  /**
   * Ofertas por las que la persona ya registró interés (`detalle.intereses`). Es la
   * única fuente de los `idOferta` al retomar una inscripción "En proceso": puede traer
   * varias (un seminario por oferta en Actualización profesional).
   */
  intereses: InscripcionOfertaResumen[];
  pagoPendiente: InscripcionPendingPaymentDetail | null;
  seniaMinima: InscripcionMinimumDeposit | null;
  confirmada: InscripcionConfirmedDetail | null;
}

// El flujo de pago/confirmación pinta la pantalla desde el preEnrollmentResponse del
// store. Al retomar una inscripción desde el panel reconstruimos esa misma forma a
// partir del detalle (seña, vencimiento y resumen carrera/comienzo/turno) para que
// el monto, la fecha y el resumen se muestren sin tener que rehacer la preinscripción.
export function detailToPreEnrollment(
  detail: InscripcionDetail
): InscripcionPreEnrollmentResponse | null {
  // El bloque seniaMinima (seña ya elegida) no trae resumen ni vencimiento; solo el
  // monto y la persona. Igual reconstruimos el preEnrollment para mostrar el monto.
  const source = detail.pagoPendiente ?? detail.confirmada;
  if (!source && !detail.seniaMinima) return null;

  const resumen = source?.resumen ?? null;
  return {
    idInscripcion: detail.pagoPendiente?.idInscripcion ?? null,
    confirmada: detail.confirmada !== null,
    fechaVencimientoPago: detail.pagoPendiente?.fechaVencimientoPago ?? null,
    seniaInscripcion: detail.pagoPendiente?.senia ?? detail.seniaMinima?.senia ?? null,
    saldoCuenta: detail.pagoPendiente?.saldoCuenta ?? null,
    resumen: resumen
      ? { carrera: resumen.carrera, comienzo: resumen.comienzo, turno: resumen.turno }
      : null,
    seminarios: detail.pagoPendiente?.seminarios ?? [],
  };
}
