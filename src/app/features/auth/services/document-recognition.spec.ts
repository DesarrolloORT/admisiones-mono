import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { DocumentRecognition } from './document-recognition';

describe('DocumentRecognition', () => {
  let service: DocumentRecognition;
  let endpointMock: {
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      recognizeDocument: vi.fn().mockReturnValue(of({ requiereRevision: false })),
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
      expect(response.requiereRevision).toBe(false);
    });

    expect(endpointMock.recognizeDocument).toHaveBeenCalledWith(payload);
  });

  it('should propagate errors from the endpoint adapter', () => {
    const requestError = new Error('request failed');
    endpointMock.recognizeDocument.mockReturnValueOnce(throwError(() => requestError));

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
          expect(error).toBe(requestError);
        },
      });
  });
});
