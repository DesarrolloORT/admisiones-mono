/**
 * Inscripción confirmada que habilita el flujo de becas. Es el tipo propio de la
 * feature: el adapter colapsa la nullability del DTO generado antes de exponerlo.
 * Las fechas viajan como `string | null`; la conversión a `Date` es explícita en
 * la UI.
 */
export interface ConfirmedEnrollment {
  enrollmentId: number;
  enrollmentDate: string | null;
  status: string;
  productId: number;
  degreeProgramName: string;
  productLevelId: number | null;
  admissionProcessId: number;
  intakeId: number;
  intakeName: string;
  intakeStartDate: string | null;
  shiftId: number;
  shiftName: string;
  offeringId: number;
  origin: string;
}
