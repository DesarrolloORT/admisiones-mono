import type { EnrollmentOfferingSummary, EnrollmentPreEnrollmentResponse } from './enrollment-flow';

// La cabecera del Detalle (`DtoCabeceraEnrollment`) solo trae producto y degreeProgram; el
// intake y el shift llegan como texto en la oferta asociada. La API NO expone
// `idComienzo` ni `idTurno` en ningún bloque del Detalle: no los declaramos para que el
// contrato no prometa datos que nunca llegan (fue la causa del bug de retomar AP).
export interface EnrollmentSummary {
  offeringId: number | null;
  productId: number | null;
  degreeProgram: string | null;
  intake: string | null;
  shift: string | null;
}

export interface EnrollmentPendingPaymentDetail {
  idEnrollment: number | null;
  deposit: number | null;
  accountBalance: number | null;
  paymentDueDate: string | null;
  summary: EnrollmentSummary | null;
  seminars: EnrollmentOfferingSummary[];
}

export interface EnrollmentCoordinator {
  name: string | null;
  email: string | null;
}

export interface EnrollmentMinimumDeposit {
  paymentMethod: string | null;
  documentNumber: string | null;
  personCode: number | null;
  deposit: number | null;
}

export interface EnrollmentSubject {
  subjectId: number | null;
  name: string | null;
}

// Una por cada oferta confirmed, con su propio intake/shift/materias: en
// Actualización profesional (niveles 3 y 4) vienen varias, una por seminario.
export interface EnrollmentConfirmedEnrollment {
  idEnrollment: number | null;
  offeringId: number | null;
  intake: string | null;
  shift: string | null;
  firstSemesterSubjects: EnrollmentSubject[];
}

// `confirmed` es una cabecera compartida (producto, degreeProgram y coordinación) más el
// summary por oferta confirmed; el intake/shift del summary sale de la primera.
export interface EnrollmentConfirmedDetail {
  studentNumber: number | null;
  summary: EnrollmentSummary | null;
  academicCoordinator: EnrollmentCoordinator | null;
  courseCoordinator: EnrollmentCoordinator | null;
  enrollments: EnrollmentConfirmedEnrollment[];
}

export interface EnrollmentDetail {
  status: string | null;
  summary: EnrollmentSummary | null;
  /**
   * Ofertas por las que la persona ya registró interés (`summary.interests`). Es la
   * única fuente de los `offeringId` al retomar una inscripción "En proceso": puede traer
   * varias (un seminario por oferta en Actualización profesional).
   */
  interests: EnrollmentOfferingSummary[];
  pendingPayment: EnrollmentPendingPaymentDetail | null;
  minimumDeposit: EnrollmentMinimumDeposit | null;
  confirmed: EnrollmentConfirmedDetail | null;
}

// El flujo de pago/confirmación pinta la pantalla desde el preEnrollmentResponse del
// store. Al retomar una inscripción desde el panel reconstruimos esa misma forma a
// partir del summary (seña, vencimiento y summary degreeProgram/intake/shift) para que
// el monto, la fecha y el summary se muestren sin tener que rehacer la preinscripción.
export function detailToPreEnrollment(
  detail: EnrollmentDetail
): EnrollmentPreEnrollmentResponse | null {
  // El bloque minimumDeposit (seña ya elegida) no trae summary ni vencimiento; solo el
  // monto y la persona. Igual reconstruimos el preEnrollment para mostrar el monto.
  const source = detail.pendingPayment ?? detail.confirmed;
  if (!source && !detail.minimumDeposit) return null;

  const summary = source?.summary ?? null;
  return {
    idEnrollment: detail.pendingPayment?.idEnrollment ?? null,
    confirmed: detail.confirmed !== null,
    paymentDueDate: detail.pendingPayment?.paymentDueDate ?? null,
    enrollmentDeposit: detail.pendingPayment?.deposit ?? detail.minimumDeposit?.deposit ?? null,
    accountBalance: detail.pendingPayment?.accountBalance ?? null,
    summary: summary
      ? { degreeProgram: summary.degreeProgram, intake: summary.intake, shift: summary.shift }
      : null,
    seminars: detail.pendingPayment?.seminars ?? [],
  };
}
