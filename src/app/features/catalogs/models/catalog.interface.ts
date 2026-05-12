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

export interface Country extends CatalogItem {
  code?: string;
}

export interface ReasonForChoice extends CatalogItem {}

export interface AdvertisingChoice extends CatalogItem {}

export interface Baccalaureate extends CatalogItem {}

export interface BaccalaureateYear extends CatalogItem {
  year: number;
}

export interface Institution extends CatalogItem {
  country?: string;
}

export interface University extends CatalogItem {
  country?: string;
}

export interface ScholarshipProduct extends CatalogItem {}

export interface ScholarshipFund extends CatalogItem {
  productId: number | string;
}

