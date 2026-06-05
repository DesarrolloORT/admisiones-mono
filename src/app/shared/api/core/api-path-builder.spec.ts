import { buildApiPath } from './api-path-builder';

describe('buildApiPath', () => {
  it('should return paths without params unchanged', () => {
    expect(buildApiPath('/catalogos/paises', undefined)).toBe('/catalogos/paises');
  });

  it('should replace and encode path params', () => {
    expect(
      buildApiPath('/personas/{id}/archivos/{nombre}', { id: 15, nombre: 'ci frente.pdf' })
    ).toBe('/personas/15/archivos/ci%20frente.pdf');
  });

  it('should throw when a required path param is missing', () => {
    expect(() => buildApiPath('/personas/{id}', {})).toThrow('Missing path param "id"');
  });
});
