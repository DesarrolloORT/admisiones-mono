import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { DocumentRecognitionRequestError } from '../models/document-recognition-error';
import { DocumentRecognition } from './document-recognition';

describe('DocumentRecognition', () => {
  let service: DocumentRecognition;
  let endpointMock: {
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      recognizeDocument: vi.fn().mockReturnValue(of({ success: true })),
    };

    TestBed.configureTestingModule({
      providers: [DocumentRecognition, { provide: AuthEndpoint, useValue: endpointMock }],
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

    expect(endpointMock.recognizeDocument).toHaveBeenCalledWith(payload);
  });

  it('should propagate errors from the endpoint adapter', () => {
    endpointMock.recognizeDocument.mockReturnValueOnce(
      throwError(() => new DocumentRecognitionRequestError(500))
    );

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
