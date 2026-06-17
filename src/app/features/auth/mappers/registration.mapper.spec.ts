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
    location: { codigoPais: 1, codigoEstado: 10, codigoCiudad: 100 },
    direccion: 'Mercedes 1234',
    telefono1: '099123456',
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
      telefono1: '099123456',
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
      telefono1: '099123456',
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
});
