import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';

import { DocumentRecognitionRequestError } from '../models/document-recognition-error';
import { DocumentRecognitionEndpoint } from './document-recognition.endpoint';

describe('DocumentRecognitionEndpoint', () => {
  let endpoint: DocumentRecognitionEndpoint;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), DocumentRecognitionEndpoint],
    });

    endpoint = TestBed.inject(DocumentRecognitionEndpoint);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  it('should expose the configured document recognition url', () => {
    expect(endpoint.documentRecognitionUrl).toBe(
      new URL('/ReconocimientoDocumento/Reconocer', environment.API_URL).toString()
    );
  });

  it('should post the document payload to the configured endpoint', () => {
    const payload = {
      tipoMime: 'application/pdf',
      archivoAdjunto: {
        nombreArchivo: 'documento.pdf',
        archivo: 'base64-content',
      },
    };

    endpoint.recognizeDocument(payload).subscribe(response => {
      expect(response.success).toBe(true);
    });

    const request = httpController.expectOne(endpoint.documentRecognitionUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual(payload);

    request.flush({
      success: true,
    });
  });

  it('should map http errors to domain errors', () => {
    endpoint
      .recognizeDocument({
        tipoMime: 'application/pdf',
        archivoAdjunto: {
          nombreArchivo: 'documento.pdf',
          archivo: 'base64-content',
        },
      })
      .subscribe({
        error: error => {
          expect(error).toBeInstanceOf(DocumentRecognitionRequestError);
          expect(error.status).toBe(500);
        },
      });

    const request = httpController.expectOne(endpoint.documentRecognitionUrl);
    request.flush({}, { status: 500, statusText: 'Server Error' });
  });
});
