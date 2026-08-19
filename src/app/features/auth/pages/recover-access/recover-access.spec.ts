import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { AuthApi } from '../../api/auth.api';
import { RecoverAccess } from './recover-access';

describe('RecoverAccess', () => {
  let component: RecoverAccess;
  let fixture: ComponentFixture<RecoverAccess>;
  let authEndpointMock: {
    recoverPassword: ReturnType<typeof vi.fn>;
  };
  let navigateByUrlSpy: ReturnType<typeof vi.spyOn>;
  let snackbarMock: { error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authEndpointMock = {
      recoverPassword: vi.fn().mockReturnValue(of({ success: true })),
    };
    snackbarMock = { error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [RecoverAccess],
      providers: [
        provideRouter([]),
        { provide: AuthApi, useValue: authEndpointMock },
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
      firstSurname: 'Silva',
    });

    component['submit']();

    expect(authEndpointMock.recoverPassword).toHaveBeenCalledWith({
      documentType: 'CI',
      documentNumber: '1111111-1',
      firstSurname: 'Silva',
    });
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/confirmacion-correo/recuperar-acceso');
  });

  it('does not submit invalid data', () => {
    component['submit']();

    expect(authEndpointMock.recoverPassword).not.toHaveBeenCalled();
    expect(snackbarMock.error).toHaveBeenCalledWith('Revisá los campos marcados.');
  });

  it('shows a friendly error when recovery fails', () => {
    authEndpointMock.recoverPassword.mockReturnValue(throwError(() => new Error('boom')));
    component['form'].setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      firstSurname: 'Silva',
    });

    component['submit']();

    expect(snackbarMock.error).toHaveBeenCalledWith(
      'No se pudo procesar la solicitud. Intentá nuevamente.'
    );
  });

  it('does not reveal whether an account exists', () => {
    authEndpointMock.recoverPassword.mockReturnValue(
      throwError(() => ({
        status: 404,
        message: 'Account not found',
        action: 'notify',
        isOperationResult: true,
        originalError: new Error('not found'),
      }))
    );
    component['form'].setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      firstSurname: 'Silva',
    });

    component['submit']();

    expect(navigateByUrlSpy).toHaveBeenCalledWith('/confirmacion-correo/recuperar-acceso');
    expect(snackbarMock.error).not.toHaveBeenCalled();
  });
});
