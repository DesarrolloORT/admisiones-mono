import {
  toAuthRegisterPersonalData,
  toRegisterPayload,
  toVerifyIdentityPayload,
} from './registration.mapper';

describe('registration mapper', () => {
  const identity = { documentType: 'CI', documentNumber: '12345678' };
  const personal = {
    firstName: 'Ana',
    middleName: '',
    firstSurname: 'Silva',
    secondSurname: '',
    birthDate: '2000-01-01',
    sex: 'F',
    location: { countryCode: 1, stateCode: 10, cityCode: 100 },
    address: 'Mercedes 1234',
    primaryPhone: {
      iso2: 'UY',
      number: '099123456',
      numberE164: '+59899123456',
    },
    email: 'ana@example.com',
    emailConfirmation: 'ana@example.com',
  };

  it('should map personal form values to registration data', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        birthDate: new Date(2000, 0, 1),
      })
    ).toEqual({
      firstName: 'Ana',
      middleName: '',
      firstSurname: 'Silva',
      secondSurname: '',
      birthDate: '2000-01-01',
      sex: 'F',
      countryCode: 1,
      stateCode: 10,
      cityCode: 100,
      address: 'Mercedes 1234',
      primaryPhone: {
        nationalNumber: '099123456',
        iso2: 'UY',
      },
      email: 'ana@example.com',
      emailConfirmation: 'ana@example.com',
    });
  });

  it('should build and format registration payloads for the endpoint adapter', () => {
    expect(
      toRegisterPayload({
        identity,
        personal: toAuthRegisterPersonalData(personal),
      })
    ).toEqual({
      documentType: 'CI',
      documentNumber: '1234567-8',
      firstName: 'Ana',
      middleName: null,
      firstSurname: 'Silva',
      secondSurname: null,
      birthDate: '2000-01-01',
      sex: 'F',
      address: 'Mercedes 1234',
      primaryPhone: {
        nationalNumber: '099123456',
        iso2: 'UY',
      },
      email: 'ana@example.com',
      emailConfirmation: 'ana@example.com',
      countryCode: 1,
      stateCode: 10,
      cityCode: 100,
    });
  });

  it('should build verification payloads for existing-person flow', () => {
    expect(
      toVerifyIdentityPayload({ identity, firstSurname: 'Silva', email: 'ana@example.com' })
    ).toEqual({
      documentType: 'CI',
      documentNumber: '1234567-8',
      firstSurname: 'Silva',
      email: 'ana@example.com',
    });
  });

  it('should send the national number and ISO country required by backend', () => {
    expect(toAuthRegisterPersonalData(personal).primaryPhone).toEqual({
      nationalNumber: '099123456',
      iso2: 'UY',
    });

    expect(
      toAuthRegisterPersonalData({
        ...personal,
        primaryPhone: {
          iso2: 'AR',
          number: '1123456789',
          numberE164: '+541123456789',
        },
      }).primaryPhone
    ).toEqual({
      nationalNumber: '1123456789',
      iso2: 'AR',
    });
  });

  it('should convert dd/mm/yyyy display dates to ISO format', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        birthDate: '31/12/2000',
      }).birthDate
    ).toBe('2000-12-31');
  });

  it('should map empty or null birth dates to an empty string', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        birthDate: '',
      }).birthDate
    ).toBe('');

    expect(
      toAuthRegisterPersonalData({
        ...personal,
        birthDate: null,
      }).birthDate
    ).toBe('');
  });

  it('should keep an unrecognized date format trimmed as-is', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        birthDate: '  2000/01/01  ',
      }).birthDate
    ).toBe('2000/01/01');
  });

  it('should map a null primaryPhone to an empty phone payload', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        primaryPhone: null,
      }).primaryPhone
    ).toEqual({ nationalNumber: '', iso2: null });
  });

  it('should trim whitespace-only names to empty strings', () => {
    const result = toAuthRegisterPersonalData({
      ...personal,
      firstName: '   ',
      middleName: '   ',
      firstSurname: '   ',
      secondSurname: '   ',
    });

    expect(result.firstName).toBe('');
    expect(result.middleName).toBe('');
    expect(result.firstSurname).toBe('');
    expect(result.secondSurname).toBe('');
  });

  it('should map empty middleName/secondSurname to null in the register payload', () => {
    const payload = toRegisterPayload({
      identity,
      personal: toAuthRegisterPersonalData({
        ...personal,
        middleName: '',
        secondSurname: '',
      }),
    });

    expect(payload.middleName).toBeNull();
    expect(payload.secondSurname).toBeNull();
  });
});
