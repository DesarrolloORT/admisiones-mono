import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getPersonaDatosPersonaEndpoint,
  putPersonaDatosPersonaEndpoint,
} from 'src/app/shared/api/generated/endpoints/persona.endpoints';

import { PersonalDataService } from './personal-data';

describe('PersonalDataService', () => {
  let service: PersonalDataService;
  let api: { request: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { request: vi.fn() };

    TestBed.configureTestingModule({
      providers: [{ provide: ApiHttpClient, useValue: api }],
    });

    service = TestBed.inject(PersonalDataService);
  });

  it('should load and map authenticated personal data', () => {
    api.request.mockReturnValue(
      of({
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
      })
    );

    service.getPersonalData().subscribe(result => {
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

    expect(api.request).toHaveBeenCalledWith(getPersonaDatosPersonaEndpoint, {
      withCredentials: true,
      cache: false,
    });
  });

  it('should update only editable personal data fields', () => {
    api.request.mockReturnValue(of(true));

    service
      .updatePersonalData({
        countryCode: 1,
        stateCode: 10,
        cityCode: null,
        address: ' Av. 18 de Julio 1360 ',
        phone: ' 99123456 ',
        email: ' gabrielaortiz@gmail.com ',
        emailVerification: ' gabrielaortiz@gmail.com ',
      })
      .subscribe(result => expect(result).toBe(true));

    expect(api.request).toHaveBeenCalledWith(putPersonaDatosPersonaEndpoint, {
      body: {
        codigoPais: 1,
        codigoEstado: 10,
        codigoCiudad: undefined,
        direccion: 'Av. 18 de Julio 1360',
        telefono1: '99123456',
        mail: 'gabrielaortiz@gmail.com',
        verificacionMail: 'gabrielaortiz@gmail.com',
      },
      withCredentials: true,
    });
  });
});
