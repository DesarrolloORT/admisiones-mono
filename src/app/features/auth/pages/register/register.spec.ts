import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { Catalogs } from '../../../catalogs/services/catalogs';
import { RegisterFlowFacade } from '../../facades/register-flow.facade';
import { AccountService } from '../../services/account';
import { DocumentPrefillService } from '../../services/document-prefill';
import { RegistrationService } from '../../services/registration';
import { Register } from './register';

describe('Register', () => {
  let fixture: ComponentFixture<Register>;
  let component: Register;
  let registrationMock: {
    evaluateDocument: ReturnType<typeof vi.fn>;
    verifyExistingPersonIdentity: ReturnType<typeof vi.fn>;
    confirmRegistration: ReturnType<typeof vi.fn>;
  };
  let documentPrefillMock: {
    preload: ReturnType<typeof vi.fn>;
  };
  let snackbarMock: {
    show: ReturnType<typeof vi.fn>;
    success: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof vi.fn>;
  };
  let navigateByUrlSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    registrationMock = {
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
      verifyExistingPersonIdentity: vi.fn().mockReturnValue(of({ success: true })),
      confirmRegistration: vi.fn().mockReturnValue(of({ success: true })),
    };
    documentPrefillMock = {
      preload: vi.fn().mockResolvedValue({
        patch: {
          identity: { documentType: 'CI', documentNumber: '11111111' },
          personal: {
            firstName: 'Ana',
            middleName: 'Maria',
            firstSurname: 'Silva',
            secondSurname: 'Pereira',
            birthDate: '2000-01-01',
            sex: 'F',
          },
          countryCode: 1,
          birthplace: 'Montevideo / URY',
        },
        location: {
          countryCode: 1,
          stateCode: 10,
          cityCode: null,
        },
      }),
    };
    snackbarMock = {
      show: vi.fn(),
      success: vi.fn(),
      error: vi.fn(),
    };

    TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        provideRouter([]),
        { provide: RegistrationService, useValue: registrationMock },
        {
          provide: Catalogs,
          useValue: {
            getDocumentTypes: vi.fn().mockReturnValue(
              of([
                { id: 1, label: 'Cédula de identidad', code: 'CI' },
                { id: 2, label: 'Pasaporte', code: 'PS' },
                { id: 3, label: 'DNI', code: 'DNI' },
              ])
            ),
            getCountryLocations: vi.fn().mockReturnValue(of([])),
          },
        },
        { provide: DocumentPrefillService, useValue: documentPrefillMock },
        { provide: SnackbarHandler, useValue: snackbarMock },
        { provide: AccountService, useValue: { validatePhone: vi.fn().mockReturnValue(of(true)) } },
      ],
    });

    navigateByUrlSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should create the register page', () => {
    expect(component).toBeTruthy();
  });

  it('should focus #main-content and scroll to top when the step changes', () => {
    const ownerDocument = fixture.nativeElement.ownerDocument as Document;
    const mainContent = fixture.nativeElement.querySelector('#main-content') as HTMLElement;
    const focusSpy = vi.spyOn(mainContent, 'focus');
    const scrollToSpy = vi
      .spyOn(ownerDocument.defaultView as Window, 'scrollTo')
      .mockImplementation(() => undefined);

    component['focusCurrentStep']();

    expect(focusSpy).toHaveBeenCalled();
    expect(scrollToSpy).toHaveBeenCalledWith(
      expect.objectContaining({ behavior: 'instant', left: 0, top: 0 })
    );
  });

  it('should continue from identity to personal step', async () => {
    const facade = component['facade'];
    facade.identityForm.setValue({
      documentType: 'CI',
      documentNumber: '11111111',
    });

    await facade.continueToPersonalData();

    expect(registrationMock.evaluateDocument).toHaveBeenCalledWith({
      documentType: 'CI',
      documentNumber: '11111111',
    });
    expect(facade.step()).toBe('personal');
    expect(facade.stepViewModel().title).toBe('Verificación de identidad');
    expect(facade.stepViewModel().stepTitle).toBe('Verificación de identidad');
  });

  it('should preload returned document fields', async () => {
    const facade = component['facade'];
    const file = new File(['binary-content'], 'cedula.pdf', { type: 'application/pdf' });

    await facade.onDocumentSelected(file);

    expect(documentPrefillMock.preload).toHaveBeenCalledWith(file);
    expect(facade.selectedFileName()).toBe('cedula.pdf');
    expect(facade.identityForm.getRawValue()).toEqual({
      documentType: 'CI',
      documentNumber: '11111111',
    });
    expect(facade.personalForm.controls.firstName.value).toBe('Ana');
    expect(facade.personalForm.controls.firstSurname.value).toBe('Silva');
    expect(facade.personalForm.controls.location.value).toEqual({
      countryCode: 1,
      stateCode: 10,
      cityCode: null,
    });
    expect(snackbarMock.success).toHaveBeenCalledWith(
      'Datos precargados. Revisalos antes de continuar.'
    );
  });

  it('should verify identity and finish existing-person flow', async () => {
    const facade = component['facade'];
    facade.identityForm.setValue({
      documentType: 'CI',
      documentNumber: '11111111',
    });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(registrationMock.verifyExistingPersonIdentity).toHaveBeenCalledWith({
      flowId: 'flow-existing-person',
      identity: { documentType: 'CI', documentNumber: '11111111' },
      firstSurname: 'Silva',
      email: 'ana@example.com',
    });
    expect(registrationMock.confirmRegistration).not.toHaveBeenCalled();
    expect(facade.step()).toBe('personal');
    expect(facade.isCompleted()).toBe(true);
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.success).not.toHaveBeenCalled();
  });

  it('should still call verifyIdentity even if emailConfirmation differs', async () => {
    const facade = component['facade'];
    facade.identityForm.setValue({
      documentType: 'CI',
      documentNumber: '11111111',
    });
    setValidPersonalForm(facade);
    facade.personalForm.patchValue({ emailConfirmation: 'otra@example.com' });

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(registrationMock.verifyExistingPersonIdentity).toHaveBeenCalledWith({
      flowId: 'flow-existing-person',
      identity: { documentType: 'CI', documentNumber: '11111111' },
      firstSurname: 'Silva',
      email: 'ana@example.com',
    });
  });

  it('should create a new account directly from personal data', async () => {
    registrationMock.evaluateDocument.mockReturnValue(
      of({
        flowId: 'flow-new-person',
        requiresPersonCreation: true,
        requiresApplicationCreation: false,
        requiresVerification: false,
        hasExistingApplication: false,
        userExists: false,
        message: null,
      })
    );
    const facade = component['facade'];
    facade.identityForm.setValue({
      documentType: 'CI',
      documentNumber: '11111111',
    });
    setValidPersonalForm(facade);

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(registrationMock.confirmRegistration).toHaveBeenCalledWith({
      flow: 'new-person',
      flowId: 'flow-new-person',
      identity: { documentType: 'CI', documentNumber: '11111111' },
      personal: expect.objectContaining({ firstName: 'Ana' }),
    });
    expect(facade.isCompleted()).toBe(true);
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.success).not.toHaveBeenCalled();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain(
      'Cuenta creada correctamente. Revisá tu correo para obtener la contraseña.'
    );
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
      number: '099123456',
      numberE164: '+59899123456',
    },
    email: 'ana@example.com',
    emailConfirmation: 'ana@example.com',
  });
}
