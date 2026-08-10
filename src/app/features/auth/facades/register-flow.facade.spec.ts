import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { Catalogs } from '../../catalogs/services/catalogs';
import { AccountService } from '../services/account';
import { DocumentPrefillService } from '../services/document-prefill';
import { RegistrationService } from '../services/registration';
import { RegisterFlowFacade } from './register-flow.facade';

describe('RegisterFlowFacade', () => {
  let facade: RegisterFlowFacade;
  let registrationMock: {
    evaluateDocument: ReturnType<typeof vi.fn>;
    verifyExistingPersonIdentity: ReturnType<typeof vi.fn>;
    confirmRegistration: ReturnType<typeof vi.fn>;
  };
  let snackbarMock: {
    show: ReturnType<typeof vi.fn>;
    success: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof vi.fn>;
  };
  let routerMock: {
    navigate: ReturnType<typeof vi.fn>;
    navigateByUrl: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    registrationMock = {
      evaluateDocument: vi.fn().mockReturnValue(
        of({
          flowId: 'flow-existing-person',
          requiereAltaPersona: false,
          requiereAltaSolicitud: false,
          requiereVerificacion: true,
          solicitudAltaExistente: false,
          usuarioExistente: false,
          message: null,
        })
      ),
      verifyExistingPersonIdentity: vi.fn().mockReturnValue(of({ success: true })),
      confirmRegistration: vi.fn().mockReturnValue(of({ success: true })),
    };
    snackbarMock = {
      show: vi.fn(),
      success: vi.fn(),
      error: vi.fn(),
    };
    routerMock = {
      navigate: vi.fn(),
      navigateByUrl: vi.fn().mockResolvedValue(true),
    };

    TestBed.configureTestingModule({
      providers: [
        RegisterFlowFacade,
        { provide: RegistrationService, useValue: registrationMock },
        {
          provide: Catalogs,
          useValue: {
            getDocumentTypes: vi.fn().mockReturnValue(of([])),
          },
        },
        {
          provide: DocumentPrefillService,
          useValue: { preload: vi.fn() },
        },
        {
          provide: AccountService,
          useValue: { validatePhone: vi.fn().mockReturnValue(of(true)) },
        },
        {
          provide: SnackbarHandler,
          useValue: snackbarMock,
        },
        {
          provide: Router,
          useValue: routerMock,
        },
      ],
    });

    facade = TestBed.inject(RegisterFlowFacade);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should block identity step when document already has a user', async () => {
    mockEvaluation({
      usuarioExistente: true,
      message: 'Ya existe un usuario registrado con este documento.',
    });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });

    await facade.continueToPersonalData();

    expect(facade.step()).toBe('identity');
    expect(facade.registrationFlow()).toBe('user-exists');
    expect(snackbarMock.show).toHaveBeenCalledWith(
      expect.objectContaining({
        message: 'Ya existe un usuario registrado con este documento.',
        variant: 'warning',
      })
    );
  });

  it('should block identity step when application already exists', async () => {
    mockEvaluation({
      solicitudAltaExistente: true,
      message: 'Ya existe una solicitud de alta pendiente para este documento.',
    });
    facade.identityForm.setValue({ documentType: 'PS', documentNumber: 'AB123456' });

    await facade.continueToPersonalData();

    expect(facade.step()).toBe('identity');
    expect(facade.registrationFlow()).toBe('application-exists');
    expect(snackbarMock.show).toHaveBeenCalledWith(
      expect.objectContaining({
        message: 'Ya existe una solicitud de alta pendiente para este documento.',
        variant: 'warning',
      })
    );
  });

  it('should verify an existing CI person from the personal step', async () => {
    mockEvaluation({ requiereVerificacion: true });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(facade.registrationFlow()).toBe('existing-person');
    expect(facade.personalMode()).toBe('verification');
    expect(registrationMock.verifyExistingPersonIdentity).toHaveBeenCalledWith({
      flowId: 'flow-existing-person',
      identity: { documentType: 'CI', documentNumber: '11111111' },
      primerApellido: 'Silva',
      mail: 'ana@example.com',
    });
    expect(registrationMock.confirmRegistration).not.toHaveBeenCalled();
    expect(facade.step()).toBe('personal');
    expect(facade.isCompleted()).toBe(true);
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.success).not.toHaveBeenCalled();
  });

  it('should collect full data and register a new CI person from the personal step', async () => {
    mockEvaluation({ requiereAltaPersona: true, flowId: 'flow-new-person' });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(facade.registrationFlow()).toBe('new-person');
    expect(facade.personalMode()).toBe('complete');
    expect(registrationMock.confirmRegistration).toHaveBeenCalledWith({
      flow: 'new-person',
      flowId: 'flow-new-person',
      identity: { documentType: 'CI', documentNumber: '11111111' },
      personal: expect.objectContaining({
        primerNombre: 'Ana',
        primerApellido: 'Silva',
        mail: 'ana@example.com',
      }),
    });
    expect(facade.step()).toBe('personal');
    expect(facade.isCompleted()).toBe(true);
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.success).not.toHaveBeenCalled();
  });

  it('should block continuable registration when backend omits flowId', async () => {
    mockEvaluation({ requiereAltaPersona: true, flowId: null });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });

    await facade.continueToPersonalData();

    expect(facade.step()).toBe('identity');
    expect(snackbarMock.error).toHaveBeenCalledWith(
      'No se pudo iniciar el flujo de registro. Intentá nuevamente.'
    );
    expect(facade.registrationFlow()).toBe('new-person');
    expect(facade.registrationFlowId()).toBeNull();
  });

  it('should collect full data and confirm a non-CI application request', async () => {
    mockEvaluation({ requiereAltaSolicitud: true, flowId: 'flow-new-application' });
    facade.identityForm.setValue({ documentType: 'PS', documentNumber: 'AB123456' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(facade.registrationFlow()).toBe('new-application');
    expect(registrationMock.confirmRegistration).toHaveBeenCalledWith(
      expect.objectContaining({
        flow: 'new-application',
        flowId: 'flow-new-application',
        identity: { documentType: 'PS', documentNumber: 'AB123456' },
      })
    );
    expect(facade.isCompleted()).toBe(true);
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.success).not.toHaveBeenCalled();
  });

  it('should reject personal submit when no flow was evaluated', () => {
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    facade.submitPersonalData();

    expect(snackbarMock.error).toHaveBeenCalledWith('Primero evaluá el documento para continuar.');
    expect(registrationMock.confirmRegistration).not.toHaveBeenCalled();
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

    expect(snackbarMock.error).toHaveBeenCalledWith(
      'Ya existe un usuario registrado con este documento.'
    );
  });

  function mockEvaluation(
    partial: Partial<{
      requiereAltaPersona: boolean;
      requiereAltaSolicitud: boolean;
      requiereVerificacion: boolean;
      solicitudAltaExistente: boolean;
      usuarioExistente: boolean;
      flowId: string | null;
      message: string | null;
    }>
  ): void {
    registrationMock.evaluateDocument.mockReturnValue(
      of({
        flowId: 'flow-existing-person',
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
    telefono1: {
      iso2: 'UY',
      number: '099123456',
      numberE164: '+59899123456',
    },
    mail: 'ana@example.com',
    verificacionMail: 'ana@example.com',
  });
}
