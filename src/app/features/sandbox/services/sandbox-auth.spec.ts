import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';

import { SandboxAuth } from './sandbox-auth';

describe('SandboxAuth', () => {
  let auth: SandboxAuth;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SandboxAuth],
    });

    auth = TestBed.inject(SandboxAuth);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  it('should expose the configured login url', () => {
    expect(auth.loginUrl).toBe(environment.API_URL);
  });

  it('should expose the configured document recognition url', () => {
    expect(auth.documentRecognitionUrl).toBe(
      new URL('/ReconocimientoDocumento/Reconocer', environment.API_URL).toString()
    );
  });

  it('should post the login dto to the configured login url', () => {
    auth.login({ codigoPersona: 123, password: 'secret-123' }).subscribe();

    const request = httpController.expectOne(auth.loginUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual({
      codigoPersona: 123,
      password: 'secret-123',
    });

    request.flush({
      message: 'Autenticación exitosa. Los tokens han sido establecidos como cookies seguras.',
    });
  });

  it('should post the document recognition dto to the configured endpoint', () => {
    auth
      .recognizeDocument({
        tipoDocumentoEsperado: 'CI',
        tipoMime: 'application/pdf',
        archivoAdjunto: {
          nombreArchivo: 'cedula.pdf',
          archivo: 'base64-content',
        },
      })
      .subscribe();

    const request = httpController.expectOne(auth.documentRecognitionUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual({
      tipoDocumentoEsperado: 'CI',
      tipoMime: 'application/pdf',
      archivoAdjunto: {
        nombreArchivo: 'cedula.pdf',
        archivo: 'base64-content',
      },
    });

    request.flush({
      status: 'ok',
    });
  });
});

