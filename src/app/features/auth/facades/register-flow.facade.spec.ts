import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../catalogs/services/catalogs';
import { AuthRequestError } from '../models/auth-error';
import { Auth } from '../services/auth';
import { DocumentRecognition } from '../services/document-recognition';
import { RegisterDocumentStore } from '../store/register-document.store';
import { RegisterFlowFacade } from './register-flow.facade';

describe('RegisterFlowFacade', () => {
  let facade: RegisterFlowFacade;
  let authMock: {
    evaluateDocument: ReturnType<typeof vi.fn>;
    verifyIdentity: ReturnType<typeof vi.fn>;
    confirmExistingPerson: ReturnType<typeof vi.fn>;
  };
  let documentRecognitionMock: {
    createRequestFromFile: ReturnType<typeof vi.fn>;
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authMock = {
      evaluateDocument: vi.fn().mockReturnValue(
        of({
          requiereAltaPersona: true,
          requiereAltaSolicitud: true,
          requiereVerificacion: true,
          solicitudAltaExistente: false,
          usuarioExistente: false,
        })
      ),
      verifyIdentity: vi.fn().mockReturnValue(of({ success: true })),
      confirmExistingPerson: vi.fn().mockReturnValue(of({ success: true })),
    };
    documentRecognitionMock = {
      createRequestFromFile: vi.fn(),
      recognizeDocument: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        RegisterFlowFacade,
        RegisterDocumentStore,
        { provide: Auth, useValue: authMock },
        {
          provide: Catalogs,
          useValue: {
            getDocumentTypes: vi.fn().mockReturnValue(of([])),
            getCountryLocations: vi.fn().mockReturnValue(of([])),
            getCareers: vi.fn().mockReturnValue(of([])),
            getComienzos: vi.fn().mockReturnValue(of([])),
          },
        },
        { provide: DocumentRecognition, useValue: documentRecognitionMock },
      ],
    });

    facade = TestBed.inject(RegisterFlowFacade);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should block identity step when document already has a user', async () => {
    authMock.evaluateDocument.mockReturnValue(
      of({
        requiereAltaPersona: false,
        requiereAltaSolicitud: false,
        requiereVerificacion: false,
        solicitudAltaExistente: false,
        usuarioExistente: true,
      })
    );
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '12345678' });

    await facade.continueToPersonalData();

    expect(facade.step()).toBe('identity');
    expect(facade.error()).toBe('Ya existe un usuario registrado con este documento.');
  });

  it('should surface evaluate document errors', async () => {
    authMock.evaluateDocument.mockReturnValue(
      throwError(() => new AuthRequestError('evaluateDocument', 409))
    );
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '12345678' });

    await facade.continueToPersonalData();

    expect(facade.error()).toBe('No se pudo completar el registro. Error 409.');
  });

  it('should reset dependent career fields when academic level changes', () => {
    facade.careerForm.setValue({ propuestaAcademica: 1, carrera: 2, comienzo: 3 });

    facade.onPropuestaChange();

    expect(facade.careerForm.controls.carrera.value).toBeNull();
    expect(facade.careerForm.controls.comienzo.value).toBeNull();
    expect(facade.comienzos()).toEqual([]);
  });
});
