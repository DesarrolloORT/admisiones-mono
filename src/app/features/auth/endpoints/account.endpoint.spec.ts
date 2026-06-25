import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  operationResultInterceptor,
  ortApiErrorInterceptor,
  provideOrtApiErrorHandling,
} from '@desarrolloort/ngx-utils';

import { AccountEndpoint } from './account.endpoint';

describe('AccountEndpoint', () => {
  let endpoint: AccountEndpoint;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([ortApiErrorInterceptor, operationResultInterceptor])),
        provideHttpClientTesting(),
        ...provideOrtApiErrorHandling({ config: { logErrors: false } }),
      ],
    });

    endpoint = TestBed.inject(AccountEndpoint);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  it('should load and map authenticated personal data', () => {
    endpoint.getPersonalData().subscribe(result => {
      expect(result).toEqual({
        documentType: 'CI',
        documentNumber: '4123456-9',
        firstName: 'Gabriela',
        secondName: '',
        firstLastName: 'Ortiz',
        secondLastName: 'Morales',
        birthDate: '1988-05-31',
        sex: 'F',
        countryCode: 1,
        stateCode: 10,
        cityCode: 100,
        address: 'Av. 18 de Julio 1360',
        phone: '99123456',
        email: 'gabrielaortiz@gmail.com',
        emailVerification: 'gabrielaortiz@gmail.com',
      });
    });

    const req = httpController.expectOne(
      r => r.url.includes('/Persona/DatosPersona') && r.method === 'GET'
    );

    expect(req.request.withCredentials).toBe(true);

    req.flush({
      success: true,
      httpCode: 200,
      data: {
        tipoDocumento: 'CI',
        documento: '4123456-9',
        primerNombre: 'Gabriela',
        segundoNombre: '',
        primerApellido: 'Ortiz',
        segundoApellido: 'Morales',
        fechaNacimiento: '1988-05-31',
        sexo: 'F',
        codigoPais: 1,
        codigoEstado: 10,
        codigoCiudad: 100,
        direccion: 'Av. 18 de Julio 1360',
        telefono1: '99123456',
        mail: 'gabrielaortiz@gmail.com',
        verificacionMail: null,
      },
    });
  });

  it('should update only editable personal data fields', () => {
    endpoint
      .updatePersonalData({
        countryCode: 1,
        stateCode: 10,
        cityCode: undefined,
        address: 'Av. 18 de Julio 1360',
        phone: '99123456',
        email: 'gabrielaortiz@gmail.com',
        emailVerification: 'gabrielaortiz@gmail.com',
      })
      .subscribe(result => expect(result).toBe(true));

    const req = httpController.expectOne(
      r => r.url.includes('/Persona/DatosPersona') && r.method === 'PUT'
    );

    expect(req.request.body).toEqual({
      codigoPais: 1,
      codigoEstado: 10,
      codigoCiudad: undefined,
      direccion: 'Av. 18 de Julio 1360',
      telefono1: '99123456',
      mail: 'gabrielaortiz@gmail.com',
      verificacionMail: 'gabrielaortiz@gmail.com',
    });
    expect(req.request.withCredentials).toBe(true);

    req.flush({ success: true, httpCode: 200, data: true });
  });

  it('should change the authenticated account password', () => {
    endpoint
      .changePassword({
        currentPassword: 'ActualPassword1!',
        password: 'NuevaPassword1!',
      })
      .subscribe(result => expect(result).toBeUndefined());

    const req = httpController.expectOne(
      r => decodeURI(r.url).includes('/Persona/CambiarContraseña') && r.method === 'POST'
    );

    expect(req.request.body).toEqual({
      passwordActual: 'ActualPassword1!',
      passwordNueva: 'NuevaPassword1!',
    });
    expect(req.request.withCredentials).toBe(true);

    req.flush({ success: true, httpCode: 200, data: null });
  });
});
