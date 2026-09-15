import { buildApiPath } from './api-path-builder';

describe('buildApiPath', () => {
  it('should return paths without params unchanged', () => {
    expect(buildApiPath('/catalogs/countries', undefined)).toBe('/catalogs/countries');
  });

  it('should replace and encode path params', () => {
    expect(
      buildApiPath('/people/{id}/files/{fileName}', { id: 15, fileName: 'identity front.pdf' })
    ).toBe('/people/15/files/identity%20front.pdf');
  });

  it('should throw when a required path param is missing', () => {
    expect(() => buildApiPath('/people/{id}', {})).toThrow('Missing path param "id"');
  });
});
