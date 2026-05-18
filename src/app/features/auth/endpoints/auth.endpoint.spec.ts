import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { AuthRequestError } from '../models/auth-error';
import { DocumentRecognitionRequestError } from '../models/document-recognition-error';
import { AuthEndpoint } from './auth.endpoint';

describe('AuthEndpoint', () => {
  let endpoint: AuthEndpoint;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    endpoint = TestBed.inject(AuthEndpoint);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
    vi.restoreAllMocks();
  });

  describe('login', () => {
    it('should POST to /Auth/Login and return documento from response', () => {
      endpoint.login({ codigoPersona: 12345, password: 'pwd' }).subscribe(result => {
        expect(result.documento).toBe('12345678');
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Auth/Login') && r.method === 'POST'
      );

      expect(req.request.body).toEqual({ codigoPersona: 12345, password: 'pwd' });
      expect(req.request.withCredentials).toBe(true);

      req.flush({ success: true, data: { persona: { documento: '12345678' } } });
    });

    it('should return empty documento when API response has no documento', () => {
      endpoint.login({ codigoPersona: 99, password: 'x' }).subscribe(result => {
        expect(result.documento).toBe('');
      });

      const req = httpController.expectOne(r => r.url.includes('/Auth/Login'));
      req.flush({ success: true, data: { persona: {} } });
    });

    it('should throw AuthRequestError on HTTP failure', () => {
      endpoint.login({ codigoPersona: 1, password: 'bad' }).subscribe({
        error: (error: AuthRequestError) => {
          expect(error).toBeInstanceOf(AuthRequestError);
          expect(error.status).toBe(401);
        },
      });

      const req = httpController.expectOne(r => r.url.includes('/Auth/Login'));
      req.flush(null, { status: 401, statusText: 'Unauthorized' });
    });
  });

  describe('register', () => {
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

      endpoint.register(payload).subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Registro/ConfirmarNuevaPersona') && r.method === 'POST'
      );

      expect(req.request.body).toEqual(payload);
      expect(req.request.withCredentials).toBe(true);

      req.flush({ success: true });
    });

    it('should throw AuthRequestError on HTTP failure', () => {
      endpoint
        .register({
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
        })
        .subscribe({
          error: (error: AuthRequestError) => {
            expect(error).toBeInstanceOf(AuthRequestError);
            expect(error.status).toBe(400);
          },
        });

      const req = httpController.expectOne(r => r.url.includes('/Registro/ConfirmarNuevaPersona'));
      req.flush(null, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('recognizeDocument', () => {
    it('should POST to /Registro/AnalizarAdjunto and return response', () => {
      const payload = {
        tipoMime: 'application/pdf',
        archivoAdjunto: { nombreArchivo: 'doc.pdf', archivo: 'base64data' },
      };

      endpoint.recognizeDocument(payload).subscribe(response => {
        expect(response.success).toBe(true);
        expect(response.data?.requiereRevision).toBe(false);
      });

      const req = httpController.expectOne(
        r => r.url.includes('/Registro/AnalizarAdjunto') && r.method === 'POST'
      );

      expect(req.request.body).toEqual(payload);
      expect(req.request.withCredentials).toBe(true);

      req.flush({ success: true, data: { requiereRevision: false } });
    });

    it('should throw DocumentRecognitionRequestError on HTTP failure', () => {
      endpoint
        .recognizeDocument({
          tipoMime: 'image/png',
          archivoAdjunto: { nombreArchivo: 'img.png', archivo: 'abc' },
        })
        .subscribe({
          error: (error: DocumentRecognitionRequestError) => {
            expect(error).toBeInstanceOf(DocumentRecognitionRequestError);
            expect(error.status).toBe(500);
          },
        });

      const req = httpController.expectOne(r => r.url.includes('/Registro/AnalizarAdjunto'));
      req.flush(null, { status: 500, statusText: 'Internal Server Error' });
    });
  });
});

