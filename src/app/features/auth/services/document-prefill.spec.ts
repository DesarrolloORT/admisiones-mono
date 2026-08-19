import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { CatalogsApi } from '../../catalogs/api/catalogs.api';
import { AuthApi } from '../api/auth.api';
import { DocumentPrefillService } from './document-prefill';
import { DocumentRecognition } from './document-recognition';

describe('DocumentPrefillService', () => {
  let service: DocumentPrefillService;
  let documentRecognitionMock: {
    createRequestFromFile: ReturnType<typeof vi.fn>;
  };
  let authEndpointMock: {
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    documentRecognitionMock = {
      createRequestFromFile: vi.fn().mockResolvedValue({
        mimeType: 'application/pdf',
        attachment: { fileName: 'identity-document.pdf', content: 'base64' },
      }),
    };
    authEndpointMock = {
      recognizeDocument: vi.fn().mockReturnValue(
        of({
          fields: {
            documentType: 'CI',
            documentNumber: '11111111',
            firstName: 'Ana',
            firstSurname: 'Silva',
            birthplace: 'Montevideo / URY',
          },
        })
      ),
    };

    TestBed.configureTestingModule({
      providers: [
        DocumentPrefillService,
        { provide: DocumentRecognition, useValue: documentRecognitionMock },
        { provide: AuthApi, useValue: authEndpointMock },
        {
          provide: CatalogsApi,
          useValue: {
            getCountryLocations: vi.fn().mockReturnValue(
              of([
                {
                  countryCode: 1,
                  name: 'Uruguay',
                  states: [{ stateCode: 10, name: 'MONTEVIDEO' }],
                },
              ])
            ),
          },
        },
      ],
    });

    service = TestBed.inject(DocumentPrefillService);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should create a document recognition request and return form prefill data', async () => {
    const file = new File(['content'], 'identity-document.pdf', { type: 'application/pdf' });

    const result = await service.preload(file);

    expect(documentRecognitionMock.createRequestFromFile).toHaveBeenCalledWith(file);
    expect(authEndpointMock.recognizeDocument).toHaveBeenCalled();
    expect(result.patch?.identity).toEqual({ documentType: 'CI', documentNumber: '11111111' });
    expect(result.patch?.personal).toEqual({ firstName: 'Ana', firstSurname: 'Silva' });
    expect(result.location).toEqual({
      countryCode: 1,
      stateCode: 10,
      cityCode: null,
    });
  });
});
