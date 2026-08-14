import {
  getCountryCodeFromBirthplace,
  getStringValue,
  resolveStateCodeFromBirthplace,
  toDateInputValue,
  toRecognizedFormPatch,
} from './document-recognition.mapper';

describe('document recognition mapper', () => {
  const birthDate = new Date(2000, 0, 1);

  it('should map recognized fields to form patches', () => {
    expect(
      toRecognizedFormPatch({
        documentType: 'CI',
        documentNumber: '12345678',
        firstName: ' Ana ',
        birthDate: '2000-01-01T00:00:00',
        birthplace: 'Montevideo / URY',
      })
    ).toEqual({
      identity: { documentType: 'CI', documentNumber: '12345678' },
      personal: { firstName: 'Ana', birthDate: birthDate },
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
            countryCode: 1,
            name: 'Uruguay',
            states: [{ countryCode: 1, stateCode: 10, name: 'MONTEVIDEO' }],
          },
        ],
        1,
        'Montevideo / URY'
      )
    ).toBe(10);
  });

  it('should return null when there are no recognized fields', () => {
    expect(toRecognizedFormPatch(undefined)).toBeNull();
  });

  it('should filter non-string and blank values via getStringValue', () => {
    expect(getStringValue(12345)).toBeNull();
    expect(getStringValue({ foo: 'bar' })).toBeNull();
    expect(getStringValue('   ')).toBeNull();
    expect(getStringValue(' Ana ')).toBe('Ana');
  });

  it('should drop non-string recognized fields from the form patch', () => {
    expect(
      toRecognizedFormPatch({
        firstName: 12345 as unknown as string,
        middleName: '   ',
      })
    ).toEqual({
      identity: {},
      personal: {},
      countryCode: null,
      birthplace: undefined,
    });
  });

  it('should return null country code when the birthplace has no known match', () => {
    expect(getCountryCodeFromBirthplace('Unknown place')).toBeNull();
    expect(getCountryCodeFromBirthplace(null)).toBeNull();
    expect(getCountryCodeFromBirthplace(undefined)).toBeNull();
  });

  it('should resolve Uruguay from a URY birthplace', () => {
    expect(getCountryCodeFromBirthplace('Montevideo / URY')).toBe(1);
  });

  it('should return null for an invalid or null date input value', () => {
    expect(toDateInputValue(null)).toBeNull();
    expect(toDateInputValue('not-a-date')).toBeNull();
    expect(toDateInputValue('31/12/2000')).toBeNull();
  });

  it('should return null when the birthplace department does not exist in the catalog', () => {
    expect(
      resolveStateCodeFromBirthplace(
        [
          {
            countryCode: 1,
            name: 'Uruguay',
            states: [{ countryCode: 1, stateCode: 10, name: 'MONTEVIDEO' }],
          },
        ],
        1,
        'Rivera / URY'
      )
    ).toBeNull();
  });
});
