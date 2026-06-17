import {
  getCountryCodeFromBirthplace,
  resolveStateCodeFromBirthplace,
  toDateInputValue,
  toRecognizedFormPatch,
} from './document-recognition.mapper';

describe('document recognition mapper', () => {
  const birthDate = new Date(2000, 0, 1);

  it('should map recognized fields to form patches', () => {
    expect(
      toRecognizedFormPatch({
        tipoDocumento: 'CI',
        numeroDocumento: '12345678',
        primerNombre: ' Ana ',
        fechaNacimiento: '2000-01-01T00:00:00',
        lugarNacimiento: 'Montevideo / URY',
      })
    ).toEqual({
      identity: { documentType: 'CI', documentNumber: '12345678' },
      personal: { primerNombre: 'Ana', fechaNacimiento: birthDate },
      countryCode: 1,
      birthplace: 'Montevideo / URY',
    });
  });

  it('should resolve country, state and date values', () => {
    expect(getCountryCodeFromBirthplace('Buenos Aires / ARG')).toBe(9);
    expect(toDateInputValue('2000-01-01T00:00:00')).toEqual(birthDate);
    expect(
      resolveStateCodeFromBirthplace(
        [
          {
            codigoPais: 1,
            nombre: 'Uruguay',
            estado: [{ codigoPais: 1, codigoEstado: 10, nombre: 'MONTEVIDEO' }],
          },
        ],
        1,
        'Montevideo / URY'
      )
    ).toBe(10);
  });
});
