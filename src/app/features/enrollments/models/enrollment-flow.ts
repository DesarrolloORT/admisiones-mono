import type { EnrollmentConfirmedDetail } from './enrollment-detail';

export type SurveySectionId =
  | 'education'
  | 'academic-decision'
  | 'ort-experience'
  | 'work-situation'
  | 'identity'
  | 'regulation';

export type SurveySectionStatus = 'pending' | 'active' | 'complete';

export type PaymentMethod =
  'bank-account' | 'personal-account' | 'banred' | 'geopay' | 'abitab' | 'paganza';

export type ApiPaymentMethod =
  'CUENTA_PERSONAL' | 'ABITAB' | 'PAGANZA' | 'BANRED' | 'GEOPAY' | 'SISTARBANC';

export type PaymentResult = 'confirmed' | 'reserved' | 'in-progress';

export interface EnrollmentInitialSurvey {
  degreeProgramId: number | null;
  intakeId: number | null;
  shiftId: number | null;
  productLevelId: number | null;
  complete: boolean;
  activeSection: SurveySectionId | null;
  studiesHighSchool: boolean | null;
  highSchoolOrientationId: number | null;
  highSchoolYearId: number | null;
  repeatsHighSchoolYear: boolean | null;
  highSchoolYearRepeatCount: number | null;
  highSchoolInstitutionId: number | null;
  highSchoolLocationId: number | null;
  highSchoolInstitutionName: string | null;
  priorHigherEducationStatusId: number | null;
  motherEducationLevelId: number | null;
  fatherEducationLevelId: number | null;
  isMotherOrtGraduate: boolean | null;
  isFatherOrtGraduate: boolean | null;
  degreeProgramDecisionYearId: number | null;
  ortDecisionYearId: number | null;
  researchedOtherUniversities: boolean | null;
  decisionSupportId: number | null;
  decisionLevelId: number | null;
  hadOrtAdvising: boolean | null;
  ortAdvisingRating: number | null;
  visitedOrtWebsite: boolean | null;
  ortWebsiteRating: number | null;
  visitedOrtCampus: boolean | null;
  ortCampusRating: number | null;
  recallsOrtAdvertising: boolean | null;
}
export interface EnrollmentInitialSurveyResponse {
  isEligibleForSurvey: boolean;
  survey: EnrollmentInitialSurvey | null;
  consideredUniversities: number[];
  otherConsideredUniversities: string[];
  higherEducationUniversities: number[];
  otherHigherEducationUniversities: string[];
  selectedReasonOptions: number[];
  selectedAdvertisingOptions: number[];
}

export interface EnrollmentInitialSurveyPayload {
  degreeProgramId: number | null;
  intakeId: number | null;
  highSchoolOrientationId: number | null;
  highSchoolYear: number | null;
  currentlyStudiesHighSchool: boolean | null;
  highSchoolYearRepeatCount: number | null;
  repeatsHighSchoolYear: boolean | null;
  fatherOrGuardianEducationLevelId: number | null;
  motherOrGuardianEducationLevelId: number | null;
  degreeProgramDecisionYearId: number | null;
  ortDecisionYearId: number | null;
  researchedOtherUniversities: boolean | null;
  otherUniversitiesInfoLine1: string | null;
  otherUniversitiesInfoLine2: string | null;
  decisionSupportId: number | null;
  highSchoolInstitutionId: number | null;
  highSchoolInstitutionName: string | null;
  finalHighSchoolYearLocationId: number | null;
  priorHigherEducationStatusId: number | null;
  decisionLevelId: number | null;
  hadOrtAdvising: boolean | null;
  ortAdvisingRatingId: number | null;
  visitedOrtWebsite: boolean | null;
  ortWebsiteRatingId: number | null;
  visitedOrtCampus: boolean | null;
  ortCampusRatingId: number | null;
  recallsOrtAdvertising: boolean | null;
  isMotherOrGuardianOrtGraduate: boolean | null;
  isFatherOrGuardianOrtGraduate: boolean | null;
  consideredUniversityIds: number[] | null;
  otherConsideredUniversities: string[] | null;
  higherEducationUniversityIds: number[] | null;
  otherHigherEducationUniversities: string[] | null;
  ortAdvertisingIds: number[] | null;
  ortChoiceReasonIds: number[] | null;
}

export interface EnrollmentConfirmPreEnrollmentPayload {
  acceptedRegulation: boolean;
  isCorporateEnrollment: boolean;
  selectedOfferingIds: number[];
}

export interface EnrollmentIdentityUploadFile {
  fileName: string;
  content: string;
}

export interface EnrollmentIdentityDocumentUploadPayload {
  date: string;
  front: EnrollmentIdentityUploadFile;
  back: EnrollmentIdentityUploadFile;
}

export interface EnrollmentIdentityPhotoUploadPayload {
  attachedFile: EnrollmentIdentityUploadFile;
}
export interface EnrollmentProductInterestPayload {
  offeringIds: number[];
  selectedAdmissionProcessId: number;
  productId: number;
}

export interface EnrollmentStudentRegulationAcceptance {
  acceptedStudentRegulation: boolean;
  acceptanceDate: string | null;
}

// Una oferta (seminario) de un paquete de Actualización profesional. `enrollmentId`
// alimenta el array `enrollmentIds` que espera el endpoint de pago.
export interface EnrollmentOfferingSummary {
  enrollmentId: number | null;
  offeringId: number | null;
  name: string | null;
  intake: string | null;
  shift: string | null;
}

export interface EnrollmentPreEnrollmentResponse {
  enrollmentId?: number | null;
  confirmed: boolean;
  isWaiting?: boolean;
  paymentDueDate: string | null;
  enrollmentDeposit: number | null;
  accountBalance: number | null;
  summary: {
    degreeProgram: string | null;
    intake: string | null;
    shift: string | null;
  } | null;
  seminars?: EnrollmentOfferingSummary[];
}

export interface EnrollmentPaymentPayload {
  enrollmentIds: number[];
  paymentMethod: PaymentMethod;
  sistarbancBankId: string | null;
}

export interface EnrollmentPaymentMessage {
  key: string | null;
  value: string | null;
}

export interface EnrollmentPaymentResponse {
  success: boolean;
  result: string | null;
  paymentUrl: string | null;
  encryptedParameters: string | null;
  messages: EnrollmentPaymentMessage[];
  confirmed: EnrollmentConfirmedDetail | null;
  message: string | null;
  errorCode: string | null;
}

// Datos que el backend informa para pagar una reserva (Abitab/Paganza). Llegan
// en el bloque seniaMinima del Detalle; la pantalla de reserva los muestra.
export interface EnrollmentReservationData {
  documentNumber: string | null;
  personCode: number | null;
}

export interface EnrollmentIdentityDocumentFile {
  content: string | null;
  fileName: string | null;
}

export interface EnrollmentIdentityDocument {
  front: EnrollmentIdentityDocumentFile | null;
  back: EnrollmentIdentityDocumentFile | null;
  expirationDate: string | null;
}
export interface EnrollmentOption {
  value: string;
  label: string;
  icon?: string;
  hint?: string;
}

export interface IdentityFiles {
  front: File | null;
  back: File | null;
  selfie: File | null;
}

export interface EnrollmentSummaryItem {
  icon: string;
  label: string;
  value: string;
}

// Fila de seminario ya formateada para el template (Actualización profesional).
export interface SeminarSummaryItem {
  enrollmentId: number | null;
  name: string;
  intake: string;
  shift: string;
}

export interface CoordinatorContact {
  role: string;
  name: string;
  email: string;
}

export interface ReservationInstructionItem {
  label: string;
  value: string;
}

export interface ReservationInstructions {
  title: string;
  description: string;
  intro: string;
  items: readonly ReservationInstructionItem[];
  help: string;
}

export interface StudentServiceLink {
  label: string;
  icon: string;
  url: string;
}

export const SURVEY_SECTIONS: readonly SurveySectionId[] = [
  'education',
  'academic-decision',
  'ort-experience',
  'identity',
  'regulation',
];

export const COMPLETE_SURVEY_SECTIONS: readonly SurveySectionId[] = ['identity', 'regulation'];

export const PROFESSIONAL_UPDATE_SURVEY_SECTIONS: readonly SurveySectionId[] = [
  'work-situation',
  'identity',
  'regulation',
];
