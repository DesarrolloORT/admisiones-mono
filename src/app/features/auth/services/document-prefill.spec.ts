import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../catalogs/services/catalogs';
import { DocumentPrefillService } from './document-prefill';
import { DocumentRecognition } from './document-recognition';

describe('DocumentPrefillService', () => {
  let service: DocumentPrefillService;
  let documentRecognitionMock: {
    createRequestFromFile: ReturnType<typeof vi.fn>;
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    documentRecognitionMock = {
      createRequestFromFile: vi.fn().mockResolvedValue({
        tipoMime: 'application/pdf',
        archivoAdjunto: { nombreArchivo: 'cedula.pdf', archivo: 'base64' },
      }),
      recognizeDocument: vi.fn().mockReturnValue(
        of({
          campos: {
            tipoDocumento: 'CI',
            numeroDocumento: '11111111',
            primerNombre: 'Ana',
            primerApellido: 'Silva',
            lugarNacimiento: 'Montevideo / URY',
          },
        })
      ),
    };

    TestBed.configureTestingModule({
      providers: [
        DocumentPrefillService,
        { provide: DocumentRecognition, useValue: documentRecognitionMock },
        {
          provide: Catalogs,
          useValue: {
            getCountryLocations: vi.fn().mockReturnValue(
              of([
                {
                  codigoPais: 1,
                  nombre: 'Uruguay',
                  estado: [{ codigoEstado: 10, nombre: 'MONTEVIDEO' }],
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
    const file = new File(['content'], 'cedula.pdf', { type: 'application/pdf' });

    const result = await service.preload(file);

    expect(documentRecognitionMock.createRequestFromFile).toHaveBeenCalledWith(file);
    expect(documentRecognitionMock.recognizeDocument).toHaveBeenCalled();
    expect(result.patch?.identity).toEqual({ documentType: 'CI', documentNumber: '11111111' });
    expect(result.patch?.personal).toEqual({ primerNombre: 'Ana', primerApellido: 'Silva' });
    expect(result.location).toEqual({
      codigoPais: 1,
      codigoEstado: 10,
      codigoCiudad: null,
    });
  });
});
