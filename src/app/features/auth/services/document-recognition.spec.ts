import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { DocumentRecognitionEndpoint } from '../endpoints/document-recognition.endpoint';
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
      providers: [
        DocumentRecognition,
        { provide: DocumentRecognitionEndpoint, useValue: endpointMock },
      ],
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
});
