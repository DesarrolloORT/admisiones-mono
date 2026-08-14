import {
  toAuthRegisterPersonalData,
  toRegisterPayload,
  toVerifyIdentityPayload,
} from './registration.mapper';

describe('registration mapper', () => {
  const identity = { documentType: 'CI', documentNumber: '12345678' };
  const personal = {
    primerNombre: 'Ana',
    segundoNombre: '',
    primerApellido: 'Silva',
    segundoApellido: '',
    fechaNacimiento: '2000-01-01',
    sexo: 'F',
    location: { countryCode: 1, stateCode: 10, cityCode: 100 },
    direccion: 'Mercedes 1234',
    telefono1: {
      iso2: 'UY',
      number: '099123456',
      numberE164: '+59899123456',
    },
    mail: 'ana@example.com',
    verificacionMail: 'ana@example.com',
  };

  it('should map personal form values to registration data', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        fechaNacimiento: new Date(2000, 0, 1),
      })
    ).toEqual({
      primerNombre: 'Ana',
      segundoNombre: '',
      primerApellido: 'Silva',
      segundoApellido: '',
      fechaNacimiento: '2000-01-01',
      sexo: 'F',
      codigoPais: 1,
      codigoEstado: 10,
      codigoCiudad: 100,
      direccion: 'Mercedes 1234',
      telefono1: {
        nationalNumber: '099123456',
        iso2: 'UY',
      },
      mail: 'ana@example.com',
      verificacionMail: 'ana@example.com',
    });
  });

  it('should build and format registration payloads for the endpoint adapter', () => {
    expect(
      toRegisterPayload({
        identity,
        personal: toAuthRegisterPersonalData(personal),
      })
    ).toEqual({
      tipoDocumento: 'CI',
      documento: '1234567-8',
      primerNombre: 'Ana',
      segundoNombre: null,
      primerApellido: 'Silva',
      segundoApellido: null,
      fechaNacimiento: '2000-01-01',
      sexo: 'F',
      direccion: 'Mercedes 1234',
      telefono1: {
        nationalNumber: '099123456',
        iso2: 'UY',
      },
      mail: 'ana@example.com',
      verificacionMail: 'ana@example.com',
      codigoPais: 1,
      codigoEstado: 10,
      codigoCiudad: 100,
    });
  });

  it('should build verification payloads for existing-person flow', () => {
    expect(
      toVerifyIdentityPayload({ identity, primerApellido: 'Silva', mail: 'ana@example.com' })
    ).toEqual({
      tipoDocumento: 'CI',
      documento: '1234567-8',
      primerApellido: 'Silva',
      mail: 'ana@example.com',
    });
  });

  it('should send the national number and ISO country required by backend', () => {
    expect(toAuthRegisterPersonalData(personal).telefono1).toEqual({
      nationalNumber: '099123456',
      iso2: 'UY',
    });

    expect(
      toAuthRegisterPersonalData({
        ...personal,
        telefono1: {
          iso2: 'AR',
          number: '1123456789',
          numberE164: '+541123456789',
        },
      }).telefono1
    ).toEqual({
      nationalNumber: '1123456789',
      iso2: 'AR',
    });
  });

  it('should convert dd/mm/yyyy display dates to ISO format', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        fechaNacimiento: '31/12/2000',
      }).fechaNacimiento
    ).toBe('2000-12-31');
  });

  it('should map empty or null birth dates to an empty string', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        fechaNacimiento: '',
      }).fechaNacimiento
    ).toBe('');

    expect(
      toAuthRegisterPersonalData({
        ...personal,
        fechaNacimiento: null,
      }).fechaNacimiento
    ).toBe('');
  });

  it('should keep an unrecognized date format trimmed as-is', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        fechaNacimiento: '  2000/01/01  ',
      }).fechaNacimiento
    ).toBe('2000/01/01');
  });

  it('should map a null telefono1 to an empty phone payload', () => {
    expect(
      toAuthRegisterPersonalData({
        ...personal,
        telefono1: null,
      }).telefono1
    ).toEqual({ nationalNumber: '', iso2: null });
  });

  it('should trim whitespace-only names to empty strings', () => {
    const result = toAuthRegisterPersonalData({
      ...personal,
      primerNombre: '   ',
      segundoNombre: '   ',
      primerApellido: '   ',
      segundoApellido: '   ',
    });

    expect(result.primerNombre).toBe('');
    expect(result.segundoNombre).toBe('');
    expect(result.primerApellido).toBe('');
    expect(result.segundoApellido).toBe('');
  });

  it('should map empty segundoNombre/segundoApellido to null in the register payload', () => {
    const payload = toRegisterPayload({
      identity,
      personal: toAuthRegisterPersonalData({
        ...personal,
        segundoNombre: '',
        segundoApellido: '',
      }),
    });

    expect(payload.segundoNombre).toBeNull();
    expect(payload.segundoApellido).toBeNull();
  });
});
