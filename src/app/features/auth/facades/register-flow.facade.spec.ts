import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { CatalogsApi } from '../../catalogs/api/catalogs.api';
import { AccountApi } from '../api/account.api';
import { AuthApi } from '../api/auth.api';
import { DocumentPrefillService } from '../services/document-prefill';
import { RegisterFlowFacade } from './register-flow.facade';

describe('RegisterFlowFacade', () => {
  let facade: RegisterFlowFacade;
  let authMock: {
    evaluateDocument: ReturnType<typeof vi.fn>;
    verifyIdentity: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
    confirmApplicationRequest: ReturnType<typeof vi.fn>;
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
  let accountMock: { validatePhone: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    accountMock = { validatePhone: vi.fn().mockReturnValue(of(true)) };
    authMock = {
      evaluateDocument: vi.fn().mockReturnValue(
        of({
          flowId: 'flow-existing-person',
          requiresPersonCreation: false,
          requiresApplicationCreation: false,
          requiresVerification: true,
          hasExistingApplication: false,
          userExists: false,
          message: null,
        })
      ),
      verifyIdentity: vi.fn().mockReturnValue(of({ mailSent: true })),
      register: vi.fn().mockReturnValue(of({ pendingReview: false, mailSent: true })),
      confirmApplicationRequest: vi
        .fn()
        .mockReturnValue(of({ pendingReview: false, mailSent: true })),
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
        { provide: AuthApi, useValue: authMock },
        {
          provide: CatalogsApi,
          useValue: {
            getDocumentTypes: vi.fn().mockReturnValue(of([])),
          },
        },
        {
          provide: DocumentPrefillService,
          useValue: { preload: vi.fn() },
        },
        {
          provide: AccountApi,
          useValue: accountMock,
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
      userExists: true,
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
      hasExistingApplication: true,
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
    expect(snackbarMock.show.mock.calls[0][0].hint).toBeUndefined();
    // Sin usuario LDAP todavia: ofrecer iniciar sesion seria un callejon sin salida.
    expect(snackbarMock.show.mock.calls[0][0].actionLabel).toBeUndefined();
  });

  it('uses a generic fallback when a terminal registration flow has no backend message', async () => {
    mockEvaluation({ userExists: true, message: null });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });

    await facade.continueToPersonalData();

    expect(snackbarMock.show).toHaveBeenCalledWith(
      expect.objectContaining({ message: 'No se pudo continuar con el registro.' })
    );
  });

  it('should verify an existing CI person from the personal step', async () => {
    mockEvaluation({ requiresVerification: true });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(facade.registrationFlow()).toBe('existing-person');
    expect(facade.personalMode()).toBe('verification');
    expect(authMock.verifyIdentity).toHaveBeenCalledWith(
      {
        documentType: 'CI',
        documentNumber: '1111111-1',
        firstSurname: 'Silva',
        email: 'ana@example.com',
      },
      'flow-existing-person'
    );
    expect(authMock.register).not.toHaveBeenCalled();
    expect(authMock.confirmApplicationRequest).not.toHaveBeenCalled();
    expect(facade.step()).toBe('personal');
    expect(facade.isCompleted()).toBe(true);
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.success).not.toHaveBeenCalled();
  });

  it('should collect full data and register a new CI person from the personal step', async () => {
    mockEvaluation({ requiresPersonCreation: true, flowId: 'flow-new-person' });
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(facade.registrationFlow()).toBe('new-person');
    expect(facade.personalMode()).toBe('complete');
    expect(authMock.register).toHaveBeenCalledWith(
      expect.objectContaining({
        documentType: 'CI',
        documentNumber: '1111111-1',
        firstName: 'Ana',
        firstSurname: 'Silva',
        email: 'ana@example.com',
      }),
      'flow-new-person'
    );
    expect(facade.step()).toBe('personal');
    expect(facade.isCompleted()).toBe(true);
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.success).not.toHaveBeenCalled();
  });

  it('should block continuable registration when backend omits flowId', async () => {
    mockEvaluation({ requiresPersonCreation: true, flowId: null });
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
    mockEvaluation({ requiresApplicationCreation: true, flowId: 'flow-new-application' });
    authMock.confirmApplicationRequest.mockReturnValue(
      of({ pendingReview: true, mailSent: false })
    );
    facade.identityForm.setValue({ documentType: 'PS', documentNumber: 'AB123456' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(facade.registrationFlow()).toBe('new-application');
    expect(authMock.confirmApplicationRequest).toHaveBeenCalledWith(
      expect.objectContaining({ documentType: 'PS', documentNumber: 'AB123456' }),
      'flow-new-application'
    );
    expect(facade.isCompleted()).toBe(true);
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith(
      '/confirmacion-correo/solicitud-registro'
    );
    expect(snackbarMock.success).not.toHaveBeenCalled();
  });

  it('should follow pendingReview instead of the document type', async () => {
    // Un CI con pendingReview termina en la pantalla de solicitud en revision:
    // la unica fuente de verdad es el backend, no el tipo de documento.
    mockEvaluation({ requiresPersonCreation: true, flowId: 'flow-new-person' });
    authMock.register.mockReturnValue(of({ pendingReview: true, mailSent: false }));
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(routerMock.navigateByUrl).toHaveBeenCalledWith(
      '/confirmacion-correo/solicitud-registro'
    );
    expect(snackbarMock.show).not.toHaveBeenCalled();
  });

  it('should offer password recovery when the activation email was not sent', async () => {
    mockEvaluation({ requiresPersonCreation: true, flowId: 'flow-new-person' });
    authMock.register.mockReturnValue(of({ pendingReview: false, mailSent: false }));
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.show).toHaveBeenCalledWith(
      expect.objectContaining({
        message: 'No pudimos enviarte el correo de activación.',
        actionLabel: 'Recuperar acceso',
        variant: 'warning',
      })
    );
  });

  it('should offer password recovery when identity verification sent no email', async () => {
    mockEvaluation({ requiresVerification: true });
    authMock.verifyIdentity.mockReturnValue(of({ mailSent: false }));
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.show).toHaveBeenCalledWith(
      expect.objectContaining({ actionLabel: 'Recuperar acceso' })
    );
  });

  it('should reject personal submit when no flow was evaluated', () => {
    facade.identityForm.setValue({ documentType: 'CI', documentNumber: '11111111' });
    setValidPersonalForm(facade);

    facade.submitPersonalData();

    expect(snackbarMock.error).toHaveBeenCalledWith('Primero evaluá el documento para continuar.');
    expect(authMock.register).not.toHaveBeenCalled();
    expect(authMock.confirmApplicationRequest).not.toHaveBeenCalled();
  });

  it('should surface evaluate document errors', async () => {
    authMock.evaluateDocument.mockReturnValue(
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
      requiresPersonCreation: boolean;
      requiresApplicationCreation: boolean;
      requiresVerification: boolean;
      hasExistingApplication: boolean;
      userExists: boolean;
      flowId: string | null;
      message: string | null;
    }>
  ): void {
    authMock.evaluateDocument.mockReturnValue(
      of({
        flowId: 'flow-existing-person',
        requiresPersonCreation: false,
        requiresApplicationCreation: false,
        requiresVerification: false,
        hasExistingApplication: false,
        userExists: false,
        message: null,
        ...partial,
      })
    );
  }
  it('should validate the phone with the country the user picked', () => {
    setValidPersonalForm(facade);

    expect(accountMock.validatePhone).toHaveBeenCalledWith({
      nationalNumber: '99123456',
      iso2: 'UY',
    });
  });

  it('blocks the phone and shows the backend message when validation fails', () => {
    accountMock.validatePhone.mockReturnValue(
      throwError(() => ({
        status: 503,
        message: 'No pudimos validar ese celular.',
        action: 'notify',
        isOperationResult: true,
        originalError: new Error('unavailable'),
      }))
    );

    setValidPersonalForm(facade);

    expect(facade.personalForm.controls.primaryPhone.hasError('phoneValidation')).toBe(true);
    expect(snackbarMock.error).toHaveBeenCalledWith('No pudimos validar ese celular.');
  });
});

function setValidPersonalForm(facade: RegisterFlowFacade): void {
  facade.personalForm.setValue({
    firstName: 'Ana',
    middleName: 'Maria',
    firstSurname: 'Silva',
    secondSurname: 'Pereira',
    birthDate: '2000-01-01',
    sex: 'F',
    location: { countryCode: 1, stateCode: 10, cityCode: 100 },
    address: 'Mercedes 1234',
    primaryPhone: {
      iso2: 'UY',
      number: '99123456',
      numberE164: '+59899123456',
    },
    email: 'ana@example.com',
    emailConfirmation: 'ana@example.com',
  });
}
