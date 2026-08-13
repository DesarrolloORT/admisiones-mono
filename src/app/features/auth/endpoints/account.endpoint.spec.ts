import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  isNormalizedApiError,
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
        provideHttpClient(
          withXhr(),
          withInterceptors([ortApiErrorInterceptor, operationResultInterceptor])
        ),
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
        phone: {
          nationalNumber: '99123456',
          iso2: 'UY',
          e164: '+59899123456',
          isValid: true,
        },
        email: 'gabrielaortiz@gmail.com',
        emailVerification: 'gabrielaortiz@gmail.com',
        identityRestricted: false,
      });
    });

    const req = httpController.expectOne(
      r => r.url.includes('/person/details') && r.method === 'GET'
    );

    expect(req.request.withCredentials).toBe(true);

    req.flush({
      success: true,
      httpCode: 200,
      data: {
        documentType: 'CI',
        documentNumber: '4123456-9',
        firstName: 'Gabriela',
        middleName: '',
        firstSurname: 'Ortiz',
        secondSurname: 'Morales',
        birthDate: '1988-05-31',
        sex: 'F',
        countryId: 1,
        stateId: 10,
        cityId: 100,
        address: 'Av. 18 de Julio 1360',
        primaryPhone: {
          isValid: true,
          e164: '+59899123456',
          iso2: 'UY',
          countryCode: 598,
          nationalNumber: '99123456',
        },
        email: 'gabrielaortiz@gmail.com',
        emailConfirmation: null,
      },
    });
  });

  it('should map missing personal data fields to safe defaults', () => {
    endpoint.getPersonalData().subscribe(result => {
      expect(result).toEqual({
        documentType: '',
        documentNumber: '',
        firstName: '',
        secondName: '',
        firstLastName: '',
        secondLastName: '',
        birthDate: '',
        sex: '',
        countryCode: null,
        stateCode: null,
        cityCode: null,
        address: '',
        phone: { nationalNumber: '', iso2: null, e164: null, isValid: false },
        email: '',
        emailVerification: '',
        identityRestricted: false,
      });
    });

    const req = httpController.expectOne(
      r => r.url.includes('/person/details') && r.method === 'GET'
    );

    req.flush({ success: true, httpCode: 200, data: {} });
  });

  it('should fall back emailVerification to mail and map identidadRestringida', () => {
    endpoint.getPersonalData().subscribe(result => {
      expect(result.emailVerification).toBe('gabrielaortiz@gmail.com');
      expect(result.identityRestricted).toBe(true);
    });

    const req = httpController.expectOne(
      r => r.url.includes('/person/details') && r.method === 'GET'
    );

    req.flush({
      success: true,
      httpCode: 200,
      data: {
        email: 'gabrielaortiz@gmail.com',
        emailConfirmation: null,
        hasRestrictedIdentity: true,
      },
    });
  });

  it('should propagate normalized API failures when loading personal data', () => {
    let caught: unknown;

    endpoint.getPersonalData().subscribe({
      error: error => {
        caught = error;
      },
    });

    const req = httpController.expectOne(
      r => r.url.includes('/person/details') && r.method === 'GET'
    );
    req.flush(null, { status: 500, statusText: 'Internal Server Error' });

    expect(isNormalizedApiError(caught)).toBe(true);
    if (isNormalizedApiError(caught)) {
      expect(caught.status).toBe(500);
    }
  });

  it('should update only editable personal data fields', () => {
    endpoint
      .updatePersonalData({
        countryCode: 1,
        stateCode: 10,
        cityCode: undefined,
        address: 'Av. 18 de Julio 1360',
        phone: { nationalNumber: '99123456', iso2: 'UY' },
        email: 'gabrielaortiz@gmail.com',
        emailVerification: 'gabrielaortiz@gmail.com',
      })
      .subscribe(result => expect(result).toBe(true));

    const req = httpController.expectOne(
      r => r.url.includes('/person/details') && r.method === 'PUT'
    );

    expect(req.request.body).toEqual({
      countryId: 1,
      stateId: 10,
      cityId: undefined,
      address: 'Av. 18 de Julio 1360',
      primaryPhone: {
        nationalNumber: '99123456',
        iso2: 'UY',
      },
      email: 'gabrielaortiz@gmail.com',
      emailConfirmation: 'gabrielaortiz@gmail.com',
    });
    expect(req.request.withCredentials).toBe(true);

    req.flush({ success: true, httpCode: 200, data: true });
  });

  it('should return false when the personal data update is rejected', () => {
    endpoint
      .updatePersonalData({
        countryCode: 1,
        stateCode: 10,
        cityCode: 100,
        address: 'Av. 18 de Julio 1360',
        phone: { nationalNumber: '99123456', iso2: 'UY' },
        email: 'gabrielaortiz@gmail.com',
        emailVerification: 'gabrielaortiz@gmail.com',
      })
      .subscribe(result => expect(result).toBe(false));

    const req = httpController.expectOne(
      r => r.url.includes('/person/details') && r.method === 'PUT'
    );

    req.flush({ success: true, httpCode: 200, data: false });
  });

  it('should propagate normalized API failures when updating personal data', () => {
    let caught: unknown;

    endpoint
      .updatePersonalData({
        address: '',
        phone: { nationalNumber: '', iso2: null },
        email: '',
        emailVerification: '',
      })
      .subscribe({
        error: error => {
          caught = error;
        },
      });

    const req = httpController.expectOne(
      r => r.url.includes('/person/details') && r.method === 'PUT'
    );
    req.flush(null, { status: 400, statusText: 'Bad Request' });

    expect(isNormalizedApiError(caught)).toBe(true);
    if (isNormalizedApiError(caught)) {
      expect(caught.status).toBe(400);
    }
  });

  it('should validate mobile phone numbers', () => {
    endpoint
      .validatePhone({
        iso2: 'UY',
        countryPrefix: 598,
        number: '99123456',
        numberE164: '+59899123456',
      })
      .subscribe(result => expect(result).toBe(true));

    const req = httpController.expectOne(
      r =>
        r.url.includes('/person/validate-phone-number') &&
        r.method === 'POST' &&
        r.params.get('isPrimaryPhone') === 'true'
    );

    expect(req.request.body).toEqual({
      e164: '+59899123456',
      iso2: 'UY',
      countryCode: 598,
      nationalNumber: '99123456',
    });
    expect(req.request.withCredentials).toBeFalsy();

    req.flush({ success: true, httpCode: 200, data: true });
  });

  it('should return false when the phone number is invalid', () => {
    endpoint
      .validatePhone({
        iso2: 'UY',
        countryPrefix: 598,
        number: '123',
        numberE164: null,
      })
      .subscribe(result => expect(result).toBe(false));

    const req = httpController.expectOne(
      r => r.url.includes('/person/validate-phone-number') && r.method === 'POST'
    );

    req.flush({ success: true, httpCode: 200, data: false });
  });

  it('should propagate normalized API failures when validating phone numbers', () => {
    let caught: unknown;

    endpoint
      .validatePhone({
        iso2: null,
        countryPrefix: null,
        number: '',
        numberE164: null,
      })
      .subscribe({
        error: error => {
          caught = error;
        },
      });

    const req = httpController.expectOne(
      r => r.url.includes('/person/validate-phone-number') && r.method === 'POST'
    );
    req.flush(null, { status: 400, statusText: 'Bad Request' });

    expect(isNormalizedApiError(caught)).toBe(true);
    if (isNormalizedApiError(caught)) {
      expect(caught.status).toBe(400);
    }
  });

  it('should change the authenticated account password', () => {
    endpoint
      .changePassword({
        currentPassword: 'ActualPassword1!',
        password: 'NuevaPassword1!',
      })
      .subscribe(result => expect(result).toBeUndefined());

    const req = httpController.expectOne(
      r => r.url.includes('/person/change-password') && r.method === 'POST'
    );

    expect(req.request.body).toEqual({
      currentPassword: 'ActualPassword1!',
      newPassword: 'NuevaPassword1!',
    });
    expect(req.request.withCredentials).toBe(true);

    req.flush({ success: true, httpCode: 200, data: null });
  });

  it('should propagate normalized API failures when changing the password', () => {
    let caught: unknown;

    endpoint
      .changePassword({
        currentPassword: 'ActualPassword1!',
        password: 'NuevaPassword1!',
      })
      .subscribe({
        error: error => {
          caught = error;
        },
      });

    const req = httpController.expectOne(
      r => r.url.includes('/person/change-password') && r.method === 'POST'
    );
    req.flush(null, { status: 400, statusText: 'Bad Request' });

    expect(isNormalizedApiError(caught)).toBe(true);
    if (isNormalizedApiError(caught)) {
      expect(caught.status).toBe(400);
    }
  });
});
