import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { AuthSessionService } from '../../services/auth-session';
import { TwoFactorValidationPage } from './two-factor-validation';

describe('TwoFactorValidationPage', () => {
  let fixture: ComponentFixture<TwoFactorValidationPage>;
  let component: TwoFactorValidationPage;
  let authSessionMock: {
    completeTwoFactor: ReturnType<typeof vi.fn>;
    resendTwoFactorCode: ReturnType<typeof vi.fn>;
  };
  let navigateByUrlSpy: ReturnType<typeof vi.spyOn>;
  let snackbarMock: { error: ReturnType<typeof vi.fn>; success: ReturnType<typeof vi.fn> };

  function setup(state: unknown = { email: 'a@b.com', sessionId: 'abc-123' }): void {
    history.replaceState(state, '');
    authSessionMock = {
      completeTwoFactor: vi
        .fn()
        .mockReturnValue(of({ documentType: 'CI', documentNumber: '12345678' })),
      resendTwoFactorCode: vi.fn().mockReturnValue(
        of({
          sessionId: 'session-456',
          maskedEmail: 'a***@example.com',
          message: 'Código reenviado.',
        })
      ),
    };
    snackbarMock = { error: vi.fn(), success: vi.fn() };
    TestBed.configureTestingModule({
      imports: [TwoFactorValidationPage],
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: authSessionMock },
        { provide: SnackbarHandler, useValue: snackbarMock },
      ],
    });

    navigateByUrlSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(TwoFactorValidationPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders the two-factor validation component inside the auth shell', () => {
    setup();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('app-two-factor-validation')).toBeTruthy();
    expect(element.textContent).toContain('Autenticación requerida');
  });

  it('redirects to login when no session id is provided', () => {
    setup({});

    expect(navigateByUrlSpy).toHaveBeenCalledWith('/iniciar-sesion');
  });

  it('restores email and session from history state', () => {
    setup({ email: 'john.doe@example.com', sessionId: 'abc-123' });

    expect(component['email']()).toBe('john.doe@example.com');
  });

  it('completes 2FA via auth session and navigates home on success', () => {
    setup({
      email: 'a@b.com',
      sessionId: 'abc-123',
      documentType: 'CI',
      documentNumber: '12345678',
    });

    component['verify']('123456');

    expect(authSessionMock.completeTwoFactor).toHaveBeenCalledWith({
      sessionId: 'abc-123',
      code: '123456',
      documentType: 'CI',
      documentNumber: '12345678',
    });
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/inicio');
  });

  it('shows a snackbar when verification fails', () => {
    setup({ email: 'a@b.com', sessionId: 'abc-123' });
    authSessionMock.completeTwoFactor.mockReturnValue(throwError(() => new Error('boom')));

    component['verify']('111111');

    expect(snackbarMock.error).toHaveBeenCalledWith(
      'No pudimos validar el código. Verificá los dígitos e intentá nuevamente.'
    );
    expect(component['isSubmitting']()).toBe(false);
  });

  it('resends the code and shows the backend message', () => {
    setup({ email: 'a@b.com', sessionId: 'abc-123' });

    component['resend']();

    expect(authSessionMock.resendTwoFactorCode).toHaveBeenCalledWith('abc-123');
    expect(component['email']()).toBe('a***@example.com');
    expect(component['isSubmitting']()).toBe(false);
    expect(snackbarMock.success).toHaveBeenCalledWith('Código reenviado.');
  });

  it('shows a snackbar when resending fails', () => {
    setup({ email: 'a@b.com', sessionId: 'abc-123' });
    authSessionMock.resendTwoFactorCode.mockReturnValue(throwError(() => new Error('boom')));

    component['resend']();

    expect(snackbarMock.error).toHaveBeenCalledWith(
      'No pudimos reenviar el código. Intentá nuevamente.'
    );
    expect(component['isSubmitting']()).toBe(false);
  });
});
