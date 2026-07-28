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
  codigoPais: number;
  codigoEstado: number;
  codigoCiudad: number;
  nombre: string;
}

export interface LocationState {
  codigoPais: number;
  codigoEstado: number;
  nombre: string;
  ciudad?: LocationCity[] | null;
}

export interface LocationCountry {
  codigoPais: number;
  nombre: string;
  estado?: LocationState[] | null;
}

export interface Career {
  idProducto: number;
  idProceso?: number | null;
  idNivelProducto: number;
  nombreProducto: string;
  nombreNivelProducto: string;
  nombreEscuela?: string;
}

export interface Comienzo {
  idProceso: number;
  nombreProceso: string;
}

export interface Turno {
  idOferta: number;
  idTurno: number;
  nombreTurno: string;
  horarioReferencia: string;
  descripcionOferta: string;
  fechaReferencia: string | null;
}

export interface Seminario {
  idOferta: number;
  idProceso: number;
  nombre: string;
  fechaComienzo: string | null;
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
  educacion: {
    ubicacionesUltimoAnioSecundaria: CatalogItem[];
    aniosBachillerato: BaccalaureateYearGroup[];
    estadosEducacionSuperiorPrevia: CatalogItem[];
    universidades: CatalogItem[];
    nivelesFormacionTutores: CatalogItem[];
  };
  decisionAcademica: {
    aniosEducacionMediaSuperior: CatalogItem[];
    apoyosDecision: CatalogItem[];
    nivelesDecision: CatalogItem[];
    universidades: CatalogItem[];
    motivosEleccionOrt: CatalogItem[];
  };
  experienciaOrt: {
    valoraciones: CatalogItem[];
    publicidadesOrt: CatalogItem[];
  };
}

/** Banco disponible para el pago de la seña. */
export interface Bank extends CatalogItem {
  code: string | null;
}

/** Institución educativa para un país/estado dados. */
export interface EducationalInstitution extends CatalogItem {
  codigoPais: number | null;
  codigoEstado: number | null;
}
