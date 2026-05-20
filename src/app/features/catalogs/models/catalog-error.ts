export type CatalogType = 'career' | 'comienzo' | 'country' | 'documentType';

export class CatalogRequestError extends Error {
  public constructor(
    public readonly catalog: CatalogType,
    public readonly status: number | null,
    public readonly detail: string | null
  ) {
    super(catalog);
  }
}
