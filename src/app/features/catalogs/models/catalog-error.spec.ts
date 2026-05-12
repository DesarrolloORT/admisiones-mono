import { CatalogRequestError } from './catalog-error';

describe('CatalogRequestError', () => {
  it('should create error with catalog and status', () => {
    const error = new CatalogRequestError('country', 404);

    expect(error.name).toBe('CatalogRequestError');
    expect(error.catalog).toBe('country');
    expect(error.status).toBe(404);
    expect(error.message).toContain('country');
    expect(error.message).toContain('404');
  });

  it('should handle null status', () => {
    const error = new CatalogRequestError('institution', null);

    expect(error.status).toBeNull();
    expect(error.message).not.toContain('HTTP');
  });
});

