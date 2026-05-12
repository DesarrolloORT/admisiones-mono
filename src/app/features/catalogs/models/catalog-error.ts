export type CatalogType =
  | 'country'
  | 'reasonForChoice'
  | 'advertisingChoice'
  | 'baccalaureate'
  | 'baccalaureateYear'
  | 'institution'
  | 'university'
  | 'scholarshipProduct'
  | 'scholarshipFund';

export class CatalogRequestError extends Error {
  public readonly catalog: CatalogType;
  public readonly status: number | null;

  constructor(catalog: CatalogType, status: number | null) {
    super(`Failed to fetch catalog "${catalog}"${status ? ` (HTTP ${status})` : ''}`);
    this.name = 'CatalogRequestError';
    this.catalog = catalog;
    this.status = status;
  }
}

