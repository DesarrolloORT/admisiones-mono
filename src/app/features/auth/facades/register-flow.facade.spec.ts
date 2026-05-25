import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { Catalogs } from '../../catalogs/services/catalogs';
import { DocumentPrefillService } from '../services/document-prefill';
import { RegistrationService } from '../services/registration';
import { RegisterFlowFacade } from './register-flow.facade';

describe('RegisterFlowFacade', () => {
  let facade: RegisterFlowFacade;
  let registrationMock: {
    evaluateDocument: ReturnType<typeof vi.fn>;
    verifyExistingPersonIdentity: ReturnType<typeof vi.fn>;
    confirmCareerInterest: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    registrationMock = {
      evaluateDocument: vi.fn().mockReturnValue(
        of({
          requiereAltaPersona: false,
          requiereAltaSolicitud: false,
          requiereVerificacion: true,
          solicitudAltaExistente: false,
          usuarioExistente: false,
          message: null,
        })
      ),
      verifyExistingPersonIdentity: vi.fn().mockReturnValue(of({ success: true })),
      confirmCareerInterest: vi.fn().mockReturnValue(of({ success: true })),
    };

    TestBed.configureTestingModule({
      providers: [
        RegisterFlowFacade,
        { provide: RegistrationService, useValue: registrationMock },
        {
          provide: Catalogs,
          useValue: {
            getDocumentTypes: vi.fn().mockReturnValue(of([])),
            getCareers: vi.fn().mockReturnValue(of([])),
            getComienzos: vi.fn().mockReturnValue(of([])),
          },
        },
        {
          provide: DocumentPrefillService,
          useValue: { preload: vi.fn() },
        },
        {
          provide: SnackbarHandler,
          useValue: {
            show: vi.fn(),
            success: vi.fn(),
            error: vi.fn(),
          },
        },
        {
          provide: Router,
          useValue: { navigate: vi.fn() },
        },
      ],
    });

    facade = TestBed.inject(RegisterFlowFacade);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should block identity step when document already has a user', async () => {
    mockEvaluation({ usuarioExistente: true });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });

    await facade.continueToPersonalData();

    expect(facade.step()).toBe('identity');
    expect(facade.registrationFlow()).toBe('user-exists');
    expect(facade.error()).toBeNull();
  });

  it('should block identity step when application already exists', async () => {
    mockEvaluation({ solicitudAltaExistente: true });
    facade.identityForm.setValue({ documentType: 'PS', documentNumber: 'AB123456' });

    await facade.continueToPersonalData();

    expect(facade.step()).toBe('identity');
    expect(facade.registrationFlow()).toBe('application-exists');
    expect(facade.error()).toBeNull();
  });

  it('should verify an existing CI person before career selection', async () => {
    mockEvaluation({ requiereVerificacion: true });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(facade.registrationFlow()).toBe('existing-person');
    expect(facade.personalMode()).toBe('verification');
    expect(registrationMock.verifyExistingPersonIdentity).toHaveBeenCalledWith({
      identity: { documentType: 'CI', documentNumber: '11111111' },
      primerApellido: 'Silva',
      mail: 'ana@example.com',
    });
    expect(facade.step()).toBe('career');
  });

  it('should confirm career data for an existing CI person', async () => {
    mockEvaluation({ requiereVerificacion: true });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    await facade.continueToPersonalData();
    setValidCareerForm(facade);

    facade.submitCareerData();

    expect(registrationMock.confirmCareerInterest).toHaveBeenCalledWith({
      flow: 'existing-person',
      identity: { documentType: 'CI', documentNumber: '11111111' },
      personal: null,
      selection: { idProducto: 20, idProceso: 30 },
    });
  });

  it('should collect full data and register a new CI person', async () => {
    mockEvaluation({ requiereAltaPersona: true });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();
    setValidCareerForm(facade);
    facade.submitCareerData();

    expect(facade.registrationFlow()).toBe('new-person');
    expect(facade.personalMode()).toBe('complete');
    expect(registrationMock.confirmCareerInterest).toHaveBeenCalledWith(
      expect.objectContaining({
        flow: 'new-person',
        identity: { documentType: 'CI', documentNumber: '11111111' },
        selection: { idProducto: 20, idProceso: 30 },
      })
    );
  });

  it('should collect full data and confirm a non-CI application request', async () => {
    mockEvaluation({ requiereAltaSolicitud: true });
    facade.identityForm.setValue({ documentType: 'PS', documentNumber: 'AB123456' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();
    setValidCareerForm(facade);
    facade.submitCareerData();

    expect(facade.registrationFlow()).toBe('new-application');
    expect(registrationMock.confirmCareerInterest).toHaveBeenCalledWith(
      expect.objectContaining({
        flow: 'new-application',
        identity: { documentType: 'PS', documentNumber: 'AB123456' },
      })
    );
  });

  it('should reject career confirmation when no flow was evaluated', () => {
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidCareerForm(facade);

    facade.submitCareerData();

    expect(facade.error()).toBe('Primero evaluá el documento para continuar.');
    expect(registrationMock.confirmCareerInterest).not.toHaveBeenCalled();
  });

  it('should surface evaluate document errors', async () => {
    registrationMock.evaluateDocument.mockReturnValue(
      throwError(() => ({
        status: 409,
        errorCode: 'USER_EXISTS',
        message: 'Ya existe un usuario registrado con este documento.',
        action: 'notify',
        isOperationResult: true,
        originalError: new Error('boom'),
      }))
    );
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });

    await facade.continueToPersonalData();

    expect(facade.error()).toBe('Ya existe un usuario registrado con este documento.');
  });

  it('should reset dependent career fields when academic level changes', () => {
    facade.careerForm.setValue({ propuestaAcademica: 1, carrera: 2, comienzo: 3 });

    facade.onPropuestaChange();

    expect(facade.careerForm.controls.carrera.value).toBeNull();
    expect(facade.careerForm.controls.comienzo.value).toBeNull();
    expect(facade.comienzos()).toEqual([]);
  });

  function mockEvaluation(
    partial: Partial<{
      requiereAltaPersona: boolean;
      requiereAltaSolicitud: boolean;
      requiereVerificacion: boolean;
      solicitudAltaExistente: boolean;
      usuarioExistente: boolean;
      message: string | null;
    }>
  ): void {
    registrationMock.evaluateDocument.mockReturnValue(
      of({
        requiereAltaPersona: false,
        requiereAltaSolicitud: false,
        requiereVerificacion: false,
        solicitudAltaExistente: false,
        usuarioExistente: false,
        message: null,
        ...partial,
      })
    );
  }
});

function setValidPersonalForm(facade: RegisterFlowFacade): void {
  facade.personalForm.setValue({
    primerNombre: 'Ana',
    segundoNombre: 'Maria',
    primerApellido: 'Silva',
    segundoApellido: 'Pereira',
    fechaNacimiento: '2000-01-01',
    sexo: 'F',
    location: { codigoPais: 1, codigoEstado: 10, codigoCiudad: 100 },
    direccion: 'Mercedes 1234',
    telefono1: '099123456',
    mail: 'ana@example.com',
    verificacionMail: 'ana@example.com',
  });
}

function setValidCareerForm(facade: RegisterFlowFacade): void {
  facade.careerForm.setValue({
    propuestaAcademica: 1,
    carrera: 20,
    comienzo: 30,
  });
}

