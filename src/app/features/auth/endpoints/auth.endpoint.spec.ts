import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  CUSTOM_ERROR_MESSAGES,
  isNormalizedApiError,
  operationResultInterceptor,
  ortApiErrorInterceptor,
  provideOrtApiErrorHandling,
  SUPPRESS_GLOBAL_ERROR,
} from '@desarrolloort/ngx-utils';

import { CAPTCHA_ACTION } from '../../../core/services/captcha-token';
import { SHOW_GLOBAL_LOADER } from '../../../shared/api/core/api-http-client';
import type { DocumentRecognitionData } from '../models/document-recognition.interface';
import { AUTH_FLOW_ID_HEADER, AuthEndpoint } from './auth.endpoint';

describe('AuthEndpoint', () => {
  let endpoint: AuthEndpoint;
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

    endpoint = TestBed.inject(AuthEndpoint);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  describe('login', () => {
    it('should POST to /auth/login and return authenticated outcome with document data', () => {
      endpoint
        .login({ documentType: 'CI', documentNumber: '12345', password: 'pwd' })
        .subscribe(result => {
          expect(result.kind).toBe('authenticated');
          if (result.kind === 'authenticated') {
            expect(result.documentNumber).toBe('12345678');
          }
        });

      const req = httpController.expectOne(
        r => r.url.includes('/auth/login') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({
        documentType: 'CI',
        documentNumber: '12345',
        password: 'pwd',
      });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('login');
      expect(req.request.context.get(SUPPRESS_GLOBAL_ERROR)).toBe(true);
      expect(req.request.context.get(CUSTOM_ERROR_MESSAGES)).toEqual({
        401: 'Credenciales inválidas.',
        429: 'Demasiados intentos. Intentá nuevamente más tarde.',
      });

      req.flush({
        success: true,
        httpCode: 200,
        data: { person: { documentNumber: '12345678', firstName: 'Ana' } },
      });
    });

    it('should return empty document number when authenticated response has no person data', () => {
      endpoint
        .login({ documentType: 'CI', documentNumber: '99', password: 'x' })
        .subscribe(result => {
          expect(result.kind).toBe('authenticated');
          if (result.kind === 'authenticated') {
            expect(result.documentNumber).toBe('');
          }
        });

      const req = httpController.expectOne(r => r.url.includes('/auth/login'));
      req.flush({ success: true, httpCode: 200, data: { person: {} } });
    });

    it('should return twoFactorRequired outcome when response includes sessionId', () => {
      endpoint
        .login({ documentType: 'CI', documentNumber: '99', password: 'x' })
        .subscribe(result => {
          expect(result.kind).toBe('twoFactorRequired');
          if (result.kind === 'twoFactorRequired') {
            expect(result.sessionId).toBe('ab4df653422a4c19be2867c08355fa27');
            expect(result.maskedEmail).toBe('c******a@gmail.******');
            expect(result.message).toContain('código de verificación');
          }
        });

      const req = httpController.expectOne(r => r.url.includes('/auth/login'));
      req.flush(
        {
          success: true,
          httpCode: 202,
          data: {
            sessionId: 'ab4df653422a4c19be2867c08355fa27',
            maskedEmail: 'c******a@gmail.******',
            message: 'Se envió un código de verificación a tu correo electrónico.',
          },
        },
        { status: 202, statusText: 'Accepted' }
      );
    });

    it('should propagate normalized API failures', () => {
      endpoint.login({ documentType: 'CI', documentNumber: '1', password: 'bad' }).subscribe({
        error: error => {
          expect(isNormalizedApiError(error)).toBe(true);
          if (!isNormalizedApiError(error)) {
            return;
          }

          expect(error.status).toBe(401);
          expect(error.message).toBe('Credenciales inválidas.');
        },
      });

      const req = httpController.expectOne(r => r.url.includes('/auth/login'));
      req.flush(null, { status: 401, statusText: 'Unauthorized' });
    });
  });

  describe('password activation', () => {
    it('should POST to /auth/activate-password-link and return void', () => {
      endpoint.activatePasswordLink({ token: 'token-123' }).subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        r => r.url.includes('/auth/activate-password-link') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ token: 'token-123' });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(SHOW_GLOBAL_LOADER)).toBe(true);

      req.flush({ success: true, httpCode: 200, data: { codigoPersona: 1 } });
    });

    it('should POST to /auth/complete-initial-password and return void', () => {
      endpoint.completePassword({ newPassword: 'NuevaPassword1!' }).subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        r => r.url.includes('/auth/complete-initial-password') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ newPassword: 'NuevaPassword1!' });
      expect(req.request.withCredentials).toBe(true);

      req.flush({
        success: true,
        httpCode: 200,
        data: { person: { documentNumber: '12345678', firstName: 'Ana' } },
      });
    });
  });

  describe('register', () => {
    it('should map document evaluation with flowId', () => {
      endpoint
        .evaluateDocument({ documentType: 'CI', documentNumber: '12345' })
        .subscribe(result => {
          expect(result).toEqual({
            flowId: 'flow-existing-person',
            requiresPersonCreation: false,
            requiresApplicationCreation: false,
            requiresVerification: true,
            hasExistingApplication: false,
            userExists: false,
            message: null,
          });
        });

      const req = httpController.expectOne(
        r => r.url.includes('/registration/evaluate-document') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ documentType: 'CI', documentNumber: '12345' });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('EvaluateDocument');

      req.flush({
        success: true,
        httpCode: 200,
        data: {
          flowId: 'flow-existing-person',
          requiresIdentityVerification: true,
        },
      });
    });

    it('should error when document evaluation returns a null data payload', () => {
      let caught: unknown;

      endpoint.evaluateDocument({ documentType: 'CI', documentNumber: '12345' }).subscribe({
        error: error => {
          caught = error;
        },
      });

      const req = httpController.expectOne(
        r => r.url.includes('/registration/evaluate-document') && r.method === 'POST'
      );

      req.flush({ success: true, httpCode: 200, data: null });

      // Current behavior: the adapter maps `data.flowId` without guarding against
      // a null data payload, so the observable errors with a TypeError.
      expect(caught).toBeInstanceOf(TypeError);
    });

    it('should POST to /registration/confirm-new-person and return success', () => {
      const payload = {
        documentType: 'CI',
        documentNumber: '12345678',
        firstName: 'Ana',
        middleName: null,
        firstSurname: 'Silva',
        secondSurname: null,
        birthDate: '2000-01-01',
        sex: 'F',
        address: 'Mercedes 1234',
        primaryPhone: { nationalNumber: '099123456', iso2: 'UY' },
        email: 'ana@example.com',
        emailConfirmation: 'ana@example.com',
      };

      endpoint.register(payload, 'flow-new-person').subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpController.expectOne(
        r => r.url.includes('/registration/confirm-new-person') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({
        documentType: 'CI',
        documentNumber: '12345678',
        firstName: 'Ana',
        middleName: null,
        firstSurname: 'Silva',
        secondSurname: null,
        birthDate: '2000-01-01',
        sex: 'F',
        countryId: undefined,
        stateId: undefined,
        cityId: undefined,
        address: 'Mercedes 1234',
        primaryPhone: {
          nationalNumber: '099123456',
          iso2: 'UY',
        },
        email: 'ana@example.com',
        emailConfirmation: 'ana@example.com',
      });
      expect(req.request.headers.get(AUTH_FLOW_ID_HEADER)).toBe('flow-new-person');
      expect(req.request.withCredentials).toBe(true);

      req.flush({ success: true, httpCode: 200, data: null });
    });

    it('should propagate normalized API failures', () => {
      endpoint
        .register(
          {
            documentType: 'CI',
            documentNumber: '1',
            firstName: 'X',
            middleName: null,
            firstSurname: 'Y',
            secondSurname: null,
            birthDate: '2000-01-01',
            sex: 'M',
            address: '',
            primaryPhone: { nationalNumber: '', iso2: null },
            email: '',
            emailConfirmation: '',
          },
          'flow-new-person'
        )
        .subscribe({
          error: error => {
            expect(isNormalizedApiError(error)).toBe(true);
            if (!isNormalizedApiError(error)) {
              return;
            }

            expect(error.status).toBe(400);
          },
        });

      const req = httpController.expectOne(r => r.url.includes('/registration/confirm-new-person'));
      req.flush(null, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('confirmApplicationRequest', () => {
    it('should POST to /registration/confirm-registration-request and return success', () => {
      const payload = {
        documentType: 'PS',
        documentNumber: 'AB123456',
        firstName: 'Ana',
        middleName: null,
        firstSurname: 'Silva',
        secondSurname: null,
        birthDate: '2000-01-01',
        sex: 'F',
        address: 'Mercedes 1234',
        primaryPhone: { nationalNumber: '099123456', iso2: 'UY' },
        email: 'ana@example.com',
        emailConfirmation: 'ana@example.com',
      };

      endpoint.confirmApplicationRequest(payload, 'flow-new-application').subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpController.expectOne(
        r => r.url.includes('/registration/confirm-registration-request') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({
        documentType: 'PS',
        documentNumber: 'AB123456',
        firstName: 'Ana',
        middleName: null,
        firstSurname: 'Silva',
        secondSurname: null,
        birthDate: '2000-01-01',
        sex: 'F',
        countryId: undefined,
        stateId: undefined,
        cityId: undefined,
        address: 'Mercedes 1234',
        primaryPhone: {
          nationalNumber: '099123456',
          iso2: 'UY',
        },
        email: 'ana@example.com',
        emailConfirmation: 'ana@example.com',
      });
      expect(req.request.headers.get(AUTH_FLOW_ID_HEADER)).toBe('flow-new-application');
      expect(req.request.withCredentials).toBe(true);

      req.flush({ success: true, httpCode: 200, data: null });
    });
  });

  describe('verifyIdentity', () => {
    it('should POST to /registration/verify-identity with X-Flow-Id and return success', () => {
      const payload = {
        documentType: 'CI',
        documentNumber: '12345678',
        firstSurname: 'Silva',
        email: 'ana@example.com',
      };

      endpoint.verifyIdentity(payload, 'flow-existing-person').subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpController.expectOne(
        r => r.url.includes('/registration/verify-identity') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({
        documentType: 'CI',
        documentNumber: '12345678',
        firstSurname: 'Silva',
        email: 'ana@example.com',
      });
      expect(req.request.headers.get(AUTH_FLOW_ID_HEADER)).toBe('flow-existing-person');
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('VerifyIdentity');

      req.flush({ success: true, httpCode: 200, data: null });
    });
  });

  describe('recognizeDocument', () => {
    it('should POST to /registration/analyze-attachment and return response', () => {
      const payload = {
        mimeType: 'application/pdf',
        attachment: { fileName: 'doc.pdf', content: 'base64data' },
      };

      endpoint.recognizeDocument(payload).subscribe(response => {
        expect(response.fields?.firstName).toBe('Ana');
      });

      const req = httpController.expectOne(
        r => r.url.includes('/registration/analyze-attachment') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({
        mimeType: 'application/pdf',
        file: { fileName: 'doc.pdf', content: 'base64data' },
      });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('AnalyzeAttachment');

      req.flush({ success: true, httpCode: 200, data: { fields: { firstName: 'Ana' } } });
    });

    it('should map only the feature fields and drop extra DTO fields', () => {
      let result: DocumentRecognitionData | undefined;

      endpoint
        .recognizeDocument({
          mimeType: 'application/pdf',
          attachment: { fileName: 'doc.pdf', content: 'base64data' },
        })
        .subscribe(response => {
          result = response;
        });

      const req = httpController.expectOne(r => r.url.includes('/registration/analyze-attachment'));

      req.flush({
        success: true,
        httpCode: 200,
        data: {
          fields: {
            documentType: 'CI',
            documentNumber: '12345678',
            firstName: 'Ana',
            middleName: 'María',
            firstSurname: 'Silva',
            secondSurname: 'Pereira',
            birthDate: '2000-01-01T00:00:00',
            birthPlace: 'Montevideo / URY',
            state: 'MONTEVIDEO',
            sex: 'F',
            expirationDate: '2030-01-01T00:00:00',
            nationality: 'URUGUAYA',
          },
        },
      });

      expect(result?.fields).toEqual({
        documentType: 'CI',
        documentNumber: '12345678',
        firstName: 'Ana',
        middleName: 'María',
        firstSurname: 'Silva',
        secondSurname: 'Pereira',
        birthDate: '2000-01-01T00:00:00',
        birthplace: 'Montevideo / URY',
        sex: 'F',
      });
      expect(result?.fields).not.toHaveProperty('state');
      expect(result?.fields).not.toHaveProperty('expirationDate');
      expect(result?.fields).not.toHaveProperty('nationality');
    });

    it('should map missing recognized fields to null', () => {
      endpoint
        .recognizeDocument({
          mimeType: 'application/pdf',
          attachment: { fileName: 'doc.pdf', content: 'base64data' },
        })
        .subscribe(response => {
          expect(response.fields).toEqual({
            documentType: 'CI',
            documentNumber: null,
            firstName: null,
            middleName: null,
            firstSurname: null,
            secondSurname: null,
            birthDate: null,
            birthplace: null,
            sex: null,
          });
        });

      const req = httpController.expectOne(r => r.url.includes('/registration/analyze-attachment'));
      req.flush({ success: true, httpCode: 200, data: { fields: { documentType: 'CI' } } });
    });

    it('should return undefined fields when the response has no recognized fields', () => {
      let result: { fields?: unknown } | undefined;

      endpoint
        .recognizeDocument({
          mimeType: 'image/png',
          attachment: { fileName: 'img.png', content: 'abc' },
        })
        .subscribe(response => {
          result = response;
        });

      const req = httpController.expectOne(r => r.url.includes('/registration/analyze-attachment'));
      req.flush({ success: true, httpCode: 200, data: {} });

      expect(result).toBeDefined();
      expect(result?.fields).toBeUndefined();
    });

    it('should propagate normalized API failures', () => {
      endpoint
        .recognizeDocument({
          mimeType: 'image/png',
          attachment: { fileName: 'img.png', content: 'abc' },
        })
        .subscribe({
          error: error => {
            expect(isNormalizedApiError(error)).toBe(true);
            if (!isNormalizedApiError(error)) {
              return;
            }

            expect(error.status).toBe(500);
          },
        });

      const req = httpController.expectOne(r => r.url.includes('/registration/analyze-attachment'));
      req.flush(null, { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('recoverPassword', () => {
    it('should POST to /auth/recover-password with captcha and return void', () => {
      const payload = {
        documentType: 'CI',
        documentNumber: '12345678',
        firstSurname: 'Silva',
      };

      endpoint.recoverPassword(payload).subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        r => r.url.includes('/auth/recover-password') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({
        documentType: 'CI',
        documentNumber: '12345678',
        firstSurname: 'Silva',
      });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('RecoverPassword');

      req.flush({ success: true, httpCode: 200, data: null });
    });
  });

  describe('refreshToken', () => {
    it('should POST to /auth/refresh-token without body and suppress global errors', () => {
      endpoint.refreshToken().subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        r => r.url.includes('/auth/refresh-token') && r.method === 'POST'
      );

      expect(req.request.body).toBeNull();
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(SUPPRESS_GLOBAL_ERROR)).toBe(true);

      req.flush({
        success: true,
        httpCode: 200,
        data: { person: { documentNumber: '12345678', firstName: 'Ana' } },
      });
    });
  });

  describe('logout', () => {
    it('should clear backend cookies', () => {
      endpoint.logout().subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        request => request.url.includes('/auth/logout') && request.method === 'POST'
      );

      expect(req.request.withCredentials).toBe(true);
      req.flush({ success: true, httpCode: 200, data: null });
    });
  });

  describe('verifyTwoFactorCode', () => {
    it('should POST to /auth/verify-two-factor-code and map persona data', () => {
      endpoint
        .verifyTwoFactorCode({ sessionId: 'session-123', code: '123456' })
        .subscribe(result => {
          expect(result).toEqual({ documentNumber: '12345678', firstName: 'Ana' });
        });

      const req = httpController.expectOne(
        r => r.url.includes('/auth/verify-two-factor-code') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ sessionId: 'session-123', code: '123456' });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBeNull();

      req.flush({
        success: true,
        httpCode: 200,
        data: { person: { documentNumber: '12345678', firstName: 'Ana' } },
      });
    });

    it('should return empty fields when the response has no persona', () => {
      endpoint
        .verifyTwoFactorCode({ sessionId: 'session-123', code: '123456' })
        .subscribe(result => {
          expect(result).toEqual({ documentNumber: '', firstName: '' });
        });

      const req = httpController.expectOne(r => r.url.includes('/auth/verify-two-factor-code'));
      req.flush({ success: true, httpCode: 200, data: {} });
    });

    it('should propagate normalized API failures for invalid codes', () => {
      let caught: unknown;

      endpoint.verifyTwoFactorCode({ sessionId: 'session-123', code: '000000' }).subscribe({
        error: error => {
          caught = error;
        },
      });

      const req = httpController.expectOne(r => r.url.includes('/auth/verify-two-factor-code'));
      req.flush(null, { status: 401, statusText: 'Unauthorized' });

      expect(isNormalizedApiError(caught)).toBe(true);
      if (isNormalizedApiError(caught)) {
        expect(caught.status).toBe(401);
      }
    });
  });

  describe('resendTwoFactorCode', () => {
    it('should POST to /auth/resend-two-factor-code and return the refreshed session', () => {
      endpoint.resendTwoFactorCode({ sessionId: 'session-123' }).subscribe(result => {
        expect(result).toEqual({
          sessionId: 'session-456',
          maskedEmail: 'a***@example.com',
          message: 'Código reenviado.',
        });
      });

      const req = httpController.expectOne(
        r => r.url.includes('/auth/resend-two-factor-code') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ sessionId: 'session-123' });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(SUPPRESS_GLOBAL_ERROR)).toBe(true);

      req.flush({
        success: true,
        httpCode: 200,
        data: {
          sessionId: 'session-456',
          maskedEmail: 'a***@example.com',
          message: 'Código reenviado.',
        },
      });
    });

    it('should keep the requested sessionId when the response omits it', () => {
      endpoint.resendTwoFactorCode({ sessionId: 'session-123' }).subscribe(result => {
        expect(result).toEqual({
          sessionId: 'session-123',
          maskedEmail: 'a***@example.com',
          message: 'Código reenviado.',
        });
      });

      const req = httpController.expectOne(r => r.url.includes('/auth/resend-two-factor-code'));
      req.flush({
        success: true,
        httpCode: 200,
        data: { maskedEmail: 'a***@example.com', message: 'Código reenviado.' },
      });
    });

    it('should propagate normalized API failures', () => {
      let caught: unknown;

      endpoint.resendTwoFactorCode({ sessionId: 'session-123' }).subscribe({
        error: error => {
          caught = error;
        },
      });

      const req = httpController.expectOne(r => r.url.includes('/auth/resend-two-factor-code'));
      req.flush(null, { status: 500, statusText: 'Internal Server Error' });

      expect(isNormalizedApiError(caught)).toBe(true);
      if (isNormalizedApiError(caught)) {
        expect(caught.status).toBe(500);
      }
    });
  });
});
