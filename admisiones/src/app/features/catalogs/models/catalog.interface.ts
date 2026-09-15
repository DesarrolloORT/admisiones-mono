/**
 * Catálogos de datos de referencia consumidos por múltiples features.
 *
 * Esta feature no representa un dominio de negocio: agrupa endpoints
 * de datos estáticos o semi-estáticos (países, géneros, instituciones, etc.)
 * que otras features consumen.
 */

/** Ítem genérico de catálogo con id y etiqueta. */
export interface CatalogItem {
  id: number | string;
  label: string;
}

export interface DocumentType extends CatalogItem {
  code: string;
}

export interface Country extends CatalogItem {
  code?: string;
}

export interface LocationCity {
  countryCode: number;
  stateCode: number;
  cityCode: number;
  name: string;
}

export interface LocationState {
  countryCode: number;
  stateCode: number;
  name: string;
  cities?: LocationCity[] | null;
}

export interface LocationCountry {
  countryCode: number;
  name: string;
  states?: LocationState[] | null;
}

export interface DegreeProgram {
  productId: number;
  admissionProcessId?: number | null;
  productLevelId: number;
  productName: string;
  productLevelName: string;
  schoolName?: string;
  /** AP: true habilita elegir varios seminarios; false deja una sola oferta. */
  hasSeminar?: boolean | null;
}

export interface Intake {
  admissionProcessId: number;
  admissionProcessName: string;
}

export interface Shift {
  offeringId: number;
  shiftId: number;
  shiftName: string;
  referenceSchedule: string;
  offeringDescription: string;
  referenceDate: string | null;
}

export interface Seminar {
  offeringId: number;
  admissionProcessId: number;
  name: string;
  startDate: string | null;
}

export type ReasonForChoice = CatalogItem;

export type AdvertisingChoice = CatalogItem;

export type Baccalaureate = CatalogItem;

export interface BaccalaureateYear extends CatalogItem {
  year: number;
}

export interface Institution extends CatalogItem {
  country?: string;
}

export interface University extends CatalogItem {
  country?: string;
}

export type ScholarshipProduct = CatalogItem;

export interface ScholarshipFund extends CatalogItem {
  productId: number | string;
}

/** Bachillerato dentro de un año, con su orientación. */
export interface BaccalaureateOption extends CatalogItem {
  orientation: string | null;
}

/** Año de bachillerato con sus bachilleratos/orientaciones asociadas. */
export interface BaccalaureateYearGroup extends CatalogItem {
  baccalaureates: BaccalaureateOption[];
}

export interface InitialSurveyCatalogs {
  education: {
    lastSecondaryYearLocations: CatalogItem[];
    highSchoolYears: BaccalaureateYearGroup[];
    previousHigherEducationOptions: CatalogItem[];
    universities: CatalogItem[];
    guardianEducationLevels: CatalogItem[];
  };
  academicDecision: {
    upperSecondaryYears: CatalogItem[];
    decisionSupports: CatalogItem[];
    decisionLevels: CatalogItem[];
    universities: CatalogItem[];
    ortChoiceReasons: CatalogItem[];
  };
  ortExperience: {
    ratings: CatalogItem[];
    ortAdvertisements: CatalogItem[];
  };
}

/** Banco disponible para el pago de la seña. */
export interface Bank extends CatalogItem {
  code: string | null;
}

/** Institución educativa para un país/estado dados. */
export interface EducationalInstitution extends CatalogItem {
  countryCode: number | null;
  stateCode: number | null;
}
