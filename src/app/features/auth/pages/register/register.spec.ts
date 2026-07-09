import { ApplicationRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { Catalogs } from '../../../catalogs/services/catalogs';
import { RegisterFlowFacade } from '../../facades/register-flow.facade';
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
    documentPrefillMock = {
      preload: vi.fn().mockResolvedValue({
        patch: {
          identity: { documentType: 'CI', documentNumber: '11111111' },
          personal: {
            primerNombre: 'Ana',
            segundoNombre: 'Maria',
            primerApellido: 'Silva',
            segundoApellido: 'Pereira',
            fechaNacimiento: '2000-01-01',
            sexo: 'F',
          },
          countryCode: 1,
          birthplace: 'Montevideo / URY',
        },
        location: {
          codigoPais: 1,
          codigoEstado: 10,
          codigoCiudad: null,
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

  it.only('should focus #main-content and scroll to top when the step changes', async () => {
    const ownerDocument = fixture.nativeElement.ownerDocument as Document;
    const mainContent = ownerDocument.createElement('div');
    mainContent.id = 'main-content';
    ownerDocument.body.appendChild(mainContent);
    const focusSpy = vi.spyOn(mainContent, 'focus');
    const scrollToSpy = vi
      .spyOn(ownerDocument.defaultView as Window, 'scrollTo')
      .mockImplementation(() => undefined);

    try {
      const facade = component['facade'];
      facade.identityForm.setValue({
        documentType: 'CI',
        documentNumber: '11111111',
      });

      const appRef = TestBed.inject(ApplicationRef);
      appRef.attachView(fixture.componentRef.hostView);

      await facade.continueToPersonalData();
      appRef.tick();
      await fixture.whenStable();
      appRef.tick();
      await fixture.whenStable();

      console.log('DEBUG focusCalls', focusSpy.mock.calls.length, 'debugRuns', component.debugRuns);

      expect(facade.step()).toBe('personal');
      expect(focusSpy).toHaveBeenCalled();
      expect(scrollToSpy).toHaveBeenCalledWith(
        expect.objectContaining({ behavior: 'instant', left: 0, top: 0 })
      );
    } finally {
      ownerDocument.body.removeChild(mainContent);
    }
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
    expect(facade.personalForm.controls.primerNombre.value).toBe('Ana');
    expect(facade.personalForm.controls.primerApellido.value).toBe('Silva');
    expect(facade.personalForm.controls.location.value).toEqual({
      codigoPais: 1,
      codigoEstado: 10,
      codigoCiudad: null,
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
      primerApellido: 'Silva',
      mail: 'ana@example.com',
    });
    expect(registrationMock.confirmRegistration).not.toHaveBeenCalled();
    expect(facade.step()).toBe('personal');
    expect(facade.isCompleted()).toBe(true);
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/confirmacion-correo/registro');
    expect(snackbarMock.success).not.toHaveBeenCalled();
  });

  it('should still call verifyIdentity even if verificacionMail differs', async () => {
    const facade = component['facade'];
    facade.identityForm.setValue({
      documentType: 'CI',
      documentNumber: '11111111',
    });
    setValidPersonalForm(facade);
    facade.personalForm.patchValue({ verificacionMail: 'otra@example.com' });

    await facade.continueToPersonalData();
    facade.submitPersonalData();

    expect(registrationMock.verifyExistingPersonIdentity).toHaveBeenCalledWith({
      flowId: 'flow-existing-person',
      identity: { documentType: 'CI', documentNumber: '11111111' },
      primerApellido: 'Silva',
      mail: 'ana@example.com',
    });
  });

  it('should create a new account directly from personal data', async () => {
    registrationMock.evaluateDocument.mockReturnValue(
      of({
        flowId: 'flow-new-person',
        requiereAltaPersona: true,
        requiereAltaSolicitud: false,
        requiereVerificacion: false,
        solicitudAltaExistente: false,
        usuarioExistente: false,
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
      personal: expect.objectContaining({ primerNombre: 'Ana' }),
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
