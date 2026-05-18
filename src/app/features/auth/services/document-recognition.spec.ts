import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client.service';
import { postRegistroAnalizarAdjuntoEndpoint } from 'src/app/shared/api/endpoints/generated/registro.endpoints';
import { vi } from 'vitest';

import { DocumentRecognitionRequestError } from '../models/document-recognition-error';
import { DocumentRecognition } from './document-recognition';

describe('DocumentRecognition', () => {
  let service: DocumentRecognition;
  let apiMock: {
    request: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    apiMock = {
      request: vi.fn().mockReturnValue(of({ success: true })),
    };

    TestBed.configureTestingModule({
      providers: [DocumentRecognition, { provide: ApiHttpClient, useValue: apiMock }],
    });

    service = TestBed.inject(DocumentRecognition);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should delegate document recognition payload to the endpoint', () => {
    const payload = {
      tipoMime: 'application/pdf',
      archivoAdjunto: {
        nombreArchivo: 'documento.pdf',
        archivo: 'base64-content',
      },
    };

    service.recognizeDocument(payload).subscribe(response => {
      expect(response.success).toBe(true);
    });

    expect(apiMock.request).toHaveBeenCalledWith(postRegistroAnalizarAdjuntoEndpoint, {
      body: payload,
      withCredentials: true,
    });
  });

  it('should map http errors to domain errors', () => {
    apiMock.request.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 500 })));

    service
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
  });
});
