import { LocationValue } from './location-value';

describe('LocationValue', () => {
  it('should represent nullable country, state and city ids', () => {
    const value: LocationValue = {
      codigoPais: null,
      codigoEstado: null,
      codigoCiudad: null,
    };

    expect(value.codigoPais).toBeNull();
  });
});
