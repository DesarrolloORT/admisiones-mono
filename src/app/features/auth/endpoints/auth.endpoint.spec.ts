import { provideHttpClient, withInterceptors } from '@angular/common/http';
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
import { ApiHttpClient, SHOW_GLOBAL_LOADER } from '../../../shared/api/core/api-http-client';
import type { DocumentRecognitionData } from '../models/document-recognition.interface';
import { AUTH_FLOW_ID_HEADER, AuthEndpoint } from './auth.endpoint';

describe('AuthEndpoint', () => {
  let endpoint: AuthEndpoint;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([ortApiErrorInterceptor, operationResultInterceptor])),
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
    it('should POST to /Auth/Login and return authenticated outcome with documento', () => {
      endpoint
        .login({ tipoDocumento: 'CI', documento: '12345', password: 'pwd' })
        .subscribe(result => {
          expect(result.kind).toBe('authenticated');
          if (result.kind === 'authenticated') {
            expect(result.documento).toBe('12345678');
          }
        });

      const req = httpController.expectOne(
        r => r.url.includes('/Auth/Login') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({
        tipoDocumento: 'CI',
        documento: '12345',
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
        data: { persona: { documento: '12345678', primerNombre: 'Ana' } },
      });
    });

    it('should return empty documento when authenticated response has no documento', () => {
      endpoint.login({ tipoDocumento: 'CI', documento: '99', password: 'x' }).subscribe(result => {
        expect(result.kind).toBe('authenticated');
        if (result.kind === 'authenticated') {
          expect(result.documento).toBe('');
        }
      });

      const req = httpController.expectOne(r => r.url.includes('/Auth/Login'));
      req.flush({ success: true, httpCode: 200, data: { persona: {} } });
    });

    it('should return twoFactorRequired outcome when response includes sessionId', () => {
      endpoint.login({ tipoDocumento: 'CI', documento: '99', password: 'x' }).subscribe(result => {
        expect(result.kind).toBe('twoFactorRequired');
        if (result.kind === 'twoFactorRequired') {
          expect(result.sessionId).toBe('ab4df653422a4c19be2867c08355fa27');
          expect(result.maskedEmail).toBe('c******a@gmail.******');
          expect(result.message).toContain('código de verificación');
        }
      });

      const req = httpController.expectOne(r => r.url.includes('/Auth/Login'));
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
      endpoint.login({ tipoDocumento: 'CI', documento: '1', password: 'bad' }).subscribe({
        error: error => {
          expect(isNormalizedApiError(error)).toBe(true);
          if (!isNormalizedApiError(error)) {
            return;
          }

          expect(error.status).toBe(401);
          expect(error.message).toBe('Credenciales inválidas.');
        },
      });

      const req = httpController.expectOne(r => r.url.includes('/Auth/Login'));
      req.flush(null, { status: 401, statusText: 'Unauthorized' });
    });
  });

  describe('password activation', () => {
    it('should POST to /Auth/ActivarLinkPassword and return void', () => {
      endpoint.activatePasswordLink({ token: 'token-123' }).subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Auth/ActivarLinkPassword') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ token: 'token-123' });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(SHOW_GLOBAL_LOADER)).toBe(true);

      req.flush({ success: true, httpCode: 200, data: { codigoPersona: 1 } });
    });

    it('should POST to /Auth/CompletarPassword and return void', () => {
      endpoint.completePassword({ passwordNueva: 'NuevaPassword1!' }).subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Auth/CompletarPassword') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ passwordNueva: 'NuevaPassword1!' });
      expect(req.request.withCredentials).toBe(true);

      req.flush({
        success: true,
        httpCode: 200,
        data: { persona: { documento: '12345678', primerNombre: 'Ana' } },
      });
    });
  });

  describe('register', () => {
    it('should map document evaluation with flowId', () => {
      endpoint.evaluateDocument({ tipoDocumento: 'CI', documento: '12345' }).subscribe(result => {
        expect(result).toEqual({
          flowId: 'flow-existing-person',
          requiereAltaPersona: false,
          requiereAltaSolicitud: false,
          requiereVerificacion: true,
          solicitudAltaExistente: false,
          usuarioExistente: false,
          message: null,
        });
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Registro/EvaluarDocumento') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ tipoDocumento: 'CI', documento: '12345' });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('EvaluarDocumento');

      req.flush({
        success: true,
        httpCode: 200,
        data: {
          flowId: 'flow-existing-person',
          requiereVerificacion: true,
        },
      });
    });

    it('should error when document evaluation returns a null data payload', () => {
      let caught: unknown;

      endpoint.evaluateDocument({ tipoDocumento: 'CI', documento: '12345' }).subscribe({
        error: error => {
          caught = error;
        },
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Registro/EvaluarDocumento') && r.method === 'POST'
      );

      req.flush({ success: true, httpCode: 200, data: null });

      // Current behavior: the adapter maps `data.flowId` without guarding against
      // a null data payload, so the observable errors with a TypeError.
      expect(caught).toBeInstanceOf(TypeError);
    });

    it('should POST to /Registro/ConfirmarNuevaPersona and return success', () => {
      const payload = {
        tipoDocumento: 'CI',
        documento: '12345678',
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
      };

      endpoint.register(payload, 'flow-new-person').subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Registro/ConfirmarNuevaPersona') && r.method === 'POST'
      );

      expect(req.request.body).toEqual(payload);
      expect(req.request.headers.get(AUTH_FLOW_ID_HEADER)).toBe('flow-new-person');
      expect(req.request.withCredentials).toBe(true);

      req.flush({ success: true, httpCode: 200, data: null });
    });

    it('should propagate normalized API failures', () => {
      endpoint
        .register(
          {
            tipoDocumento: 'CI',
            documento: '1',
            primerNombre: 'X',
            segundoNombre: null,
            primerApellido: 'Y',
            segundoApellido: null,
            fechaNacimiento: '2000-01-01',
            sexo: 'M',
            direccion: '',
            telefono1: '',
            mail: '',
            verificacionMail: '',
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

      const req = httpController.expectOne(r => r.url.includes('/Registro/ConfirmarNuevaPersona'));
      req.flush(null, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('confirmApplicationRequest', () => {
    it('should POST to /Registro/ConfirmarSolicitudAlta and return success', () => {
      const payload = {
        tipoDocumento: 'PS',
        documento: 'AB123456',
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
      };

      endpoint.confirmApplicationRequest(payload, 'flow-new-application').subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Registro/ConfirmarSolicitudAlta') && r.method === 'POST'
      );

      expect(req.request.body).toEqual(payload);
      expect(req.request.headers.get(AUTH_FLOW_ID_HEADER)).toBe('flow-new-application');
      expect(req.request.withCredentials).toBe(true);

      req.flush({ success: true, httpCode: 200, data: null });
    });
  });

  describe('verifyIdentity', () => {
    it('should POST to /Registro/VerificarIdentidad with X-Flow-Id and return success', () => {
      const payload = {
        tipoDocumento: 'CI',
        documento: '12345678',
        primerApellido: 'Silva',
        mail: 'ana@example.com',
      };

      endpoint.verifyIdentity(payload, 'flow-existing-person').subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Registro/VerificarIdentidad') && r.method === 'POST'
      );

      expect(req.request.body).toEqual(payload);
      expect(req.request.headers.get(AUTH_FLOW_ID_HEADER)).toBe('flow-existing-person');
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('VerificarIdentidad');

      req.flush({ success: true, httpCode: 200, data: null });
    });
  });

  describe('recognizeDocument', () => {
    it('should POST to /Registro/AnalizarAdjunto and return response', () => {
      const payload = {
        tipoMime: 'application/pdf',
        archivoAdjunto: { nombreArchivo: 'doc.pdf', archivo: 'base64data' },
      };

      endpoint.recognizeDocument(payload).subscribe(response => {
        expect(response.campos?.primerNombre).toBe('Ana');
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Registro/AnalizarAdjunto') && r.method === 'POST'
      );

      expect(req.request.body).toEqual(payload);
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('AnalizarAdjunto');

      req.flush({ success: true, httpCode: 200, data: { campos: { primerNombre: 'Ana' } } });
    });

    it('should map only the feature fields and drop extra DTO fields', () => {
      let result: DocumentRecognitionData | undefined;

      endpoint
        .recognizeDocument({
          tipoMime: 'application/pdf',
          archivoAdjunto: { nombreArchivo: 'doc.pdf', archivo: 'base64data' },
        })
        .subscribe(response => {
          result = response;
        });

      const req = httpController.expectOne(r => r.url.includes('/Registro/AnalizarAdjunto'));

      req.flush({
        success: true,
        httpCode: 200,
        data: {
          campos: {
            tipoDocumento: 'CI',
            numeroDocumento: '12345678',
            primerNombre: 'Ana',
            segundoNombre: 'María',
            primerApellido: 'Silva',
            segundoApellido: 'Pereira',
            fechaNacimiento: '2000-01-01T00:00:00',
            lugarNacimiento: 'Montevideo / URY',
            departamento: 'MONTEVIDEO',
            sexo: 'F',
            fechaVencimiento: '2030-01-01T00:00:00',
            nacionalidad: 'URUGUAYA',
          },
        },
      });

      expect(result?.campos).toEqual({
        tipoDocumento: 'CI',
        numeroDocumento: '12345678',
        primerNombre: 'Ana',
        segundoNombre: 'María',
        primerApellido: 'Silva',
        segundoApellido: 'Pereira',
        fechaNacimiento: '2000-01-01T00:00:00',
        lugarNacimiento: 'Montevideo / URY',
        sexo: 'F',
      });
      expect(result?.campos).not.toHaveProperty('departamento');
      expect(result?.campos).not.toHaveProperty('fechaVencimiento');
      expect(result?.campos).not.toHaveProperty('nacionalidad');
    });

    it('should map missing recognized fields to null', () => {
      endpoint
        .recognizeDocument({
          tipoMime: 'application/pdf',
          archivoAdjunto: { nombreArchivo: 'doc.pdf', archivo: 'base64data' },
        })
        .subscribe(response => {
          expect(response.campos).toEqual({
            tipoDocumento: 'CI',
            numeroDocumento: null,
            primerNombre: null,
            segundoNombre: null,
            primerApellido: null,
            segundoApellido: null,
            fechaNacimiento: null,
            lugarNacimiento: null,
            sexo: null,
          });
        });

      const req = httpController.expectOne(r => r.url.includes('/Registro/AnalizarAdjunto'));
      req.flush({ success: true, httpCode: 200, data: { campos: { tipoDocumento: 'CI' } } });
    });

    it('should return undefined campos when the response has no recognized fields', () => {
      let result: { campos?: unknown } | undefined;

      endpoint
        .recognizeDocument({
          tipoMime: 'image/png',
          archivoAdjunto: { nombreArchivo: 'img.png', archivo: 'abc' },
        })
        .subscribe(response => {
          result = response;
        });

      const req = httpController.expectOne(r => r.url.includes('/Registro/AnalizarAdjunto'));
      req.flush({ success: true, httpCode: 200, data: {} });

      expect(result).toBeDefined();
      expect(result?.campos).toBeUndefined();
    });

    it('should propagate normalized API failures', () => {
      endpoint
        .recognizeDocument({
          tipoMime: 'image/png',
          archivoAdjunto: { nombreArchivo: 'img.png', archivo: 'abc' },
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

      const req = httpController.expectOne(r => r.url.includes('/Registro/AnalizarAdjunto'));
      req.flush(null, { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('recoverPassword', () => {
    it('should POST to /Auth/RecuperarPassword with captcha and return void', () => {
      const payload = {
        tipoDocumento: 'CI',
        documento: '12345678',
        primerApellido: 'Silva',
      };

      endpoint.recoverPassword(payload).subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Auth/RecuperarPassword') && r.method === 'POST'
      );

      expect(req.request.body).toEqual(payload);
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('RecuperarPassword');

      req.flush({ success: true, httpCode: 200, data: null });
    });
  });

  describe('refreshToken', () => {
    it('should POST to /Auth/RefreshToken without body and suppress global errors', () => {
      endpoint.refreshToken().subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Auth/RefreshToken') && r.method === 'POST'
      );

      expect(req.request.body).toBeNull();
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(SUPPRESS_GLOBAL_ERROR)).toBe(true);

      req.flush({
        success: true,
        httpCode: 200,
        data: { persona: { documento: '12345678', primerNombre: 'Ana' } },
      });
    });
  });

  describe('logout', () => {
    it('should clear backend cookies and invalidate the API cache', () => {
      endpoint.logout().subscribe(result => {
        expect(result).toBeUndefined();
      });

      const req = httpController.expectOne(
        request => request.url.includes('/Auth/Logout') && request.method === 'POST'
      );

      expect(req.request.withCredentials).toBe(true);
      req.flush({ success: true, httpCode: 200, data: null });

      const api = TestBed.inject(ApiHttpClient);
      const clearCache = vi.spyOn(api, 'clearCache');

      endpoint.clearCache();

      expect(clearCache).toHaveBeenCalledOnce();
    });
  });

  describe('verifyTwoFactorCode', () => {
    it('should POST to /Auth/VerificarCodigo2FA and map persona data', () => {
      endpoint
        .verifyTwoFactorCode({ sessionId: 'session-123', codigo: '123456' })
        .subscribe(result => {
          expect(result).toEqual({ documento: '12345678', primerNombre: 'Ana' });
        });

      const req = httpController.expectOne(
        r => r.url.includes('/Auth/VerificarCodigo2FA') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ sessionId: 'session-123', codigo: '123456' });
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.context.get(CAPTCHA_ACTION)).toBe('VerificarCodigo2FA');

      req.flush({
        success: true,
        httpCode: 200,
        data: { persona: { documento: '12345678', primerNombre: 'Ana' } },
      });
    });

    it('should return empty fields when the response has no persona', () => {
      endpoint
        .verifyTwoFactorCode({ sessionId: 'session-123', codigo: '123456' })
        .subscribe(result => {
          expect(result).toEqual({ documento: '', primerNombre: '' });
        });

      const req = httpController.expectOne(r => r.url.includes('/Auth/VerificarCodigo2FA'));
      req.flush({ success: true, httpCode: 200, data: {} });
    });

    it('should propagate normalized API failures for invalid codes', () => {
      let caught: unknown;

      endpoint.verifyTwoFactorCode({ sessionId: 'session-123', codigo: '000000' }).subscribe({
        error: error => {
          caught = error;
        },
      });

      const req = httpController.expectOne(r => r.url.includes('/Auth/VerificarCodigo2FA'));
      req.flush(null, { status: 401, statusText: 'Unauthorized' });

      expect(isNormalizedApiError(caught)).toBe(true);
      if (isNormalizedApiError(caught)) {
        expect(caught.status).toBe(401);
      }
    });
  });

  describe('resendTwoFactorCode', () => {
    it('should POST to /Auth/ReenviarCodigo2FA and return the refreshed session', () => {
      endpoint.resendTwoFactorCode({ sessionId: 'session-123' }).subscribe(result => {
        expect(result).toEqual({
          sessionId: 'session-456',
          maskedEmail: 'a***@example.com',
          message: 'Código reenviado.',
        });
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Auth/ReenviarCodigo2FA') && r.method === 'POST'
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

      const req = httpController.expectOne(r => r.url.includes('/Auth/ReenviarCodigo2FA'));
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

      const req = httpController.expectOne(r => r.url.includes('/Auth/ReenviarCodigo2FA'));
      req.flush(null, { status: 500, statusText: 'Internal Server Error' });

      expect(isNormalizedApiError(caught)).toBe(true);
      if (isNormalizedApiError(caught)) {
        expect(caught.status).toBe(500);
      }
    });
  });
});
