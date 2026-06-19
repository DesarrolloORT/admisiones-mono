import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { PasswordActivationService } from '../../services/password-activation';
import { RecoverAccess } from './recover-access';

describe('RecoverAccess', () => {
  let component: RecoverAccess;
  let fixture: ComponentFixture<RecoverAccess>;
  let passwordServiceMock: {
    recoverPassword: ReturnType<typeof vi.fn>;
  };
  let navigateByUrlSpy: ReturnType<typeof vi.spyOn>;
  let snackbarMock: { error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    passwordServiceMock = {
      recoverPassword: vi.fn().mockReturnValue(of({ success: true })),
    };
    snackbarMock = { error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [RecoverAccess],
      providers: [
        provideRouter([]),
        { provide: PasswordActivationService, useValue: passwordServiceMock },
        { provide: SnackbarHandler, useValue: snackbarMock },
      ],
    });

    navigateByUrlSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(RecoverAccess);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('submits recovery data and navigates to email confirmation', () => {
    component['form'].setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      primerApellido: 'Silva',
    });

    component['submit']();

    expect(passwordServiceMock.recoverPassword).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '1111111-1',
      primerApellido: 'Silva',
    });
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/confirmacion-correo/recuperar-acceso');
  });

  it('does not submit invalid data', () => {
    component['submit']();

    expect(passwordServiceMock.recoverPassword).not.toHaveBeenCalled();
    expect(snackbarMock.error).toHaveBeenCalledWith('Revisá los campos marcados.');
  });

  it('shows a friendly error when recovery fails', () => {
    passwordServiceMock.recoverPassword.mockReturnValue(throwError(() => new Error('boom')));
    component['form'].setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      primerApellido: 'Silva',
    });

    component['submit']();

    expect(snackbarMock.error).toHaveBeenCalledWith(
      'No se pudo procesar la solicitud. Intentá nuevamente.'
    );
  });
});
