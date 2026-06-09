import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { AuthSessionService } from '../services/auth-session';
import { TwoFactorValidationFacade } from './two-factor-validation.facade';

describe('TwoFactorValidationFacade', () => {
  let authSessionMock: { completeTwoFactor: ReturnType<typeof vi.fn> };
  let routerMock: {
    getCurrentNavigation: ReturnType<typeof vi.fn>;
    navigateByUrl: ReturnType<typeof vi.fn>;
  };
  let snackbarMock: { success: ReturnType<typeof vi.fn> };

  const setupHistory = (state: unknown): void => {
    history.replaceState(state, '');
  };

  const buildFacade = (): TwoFactorValidationFacade => {
    TestBed.configureTestingModule({
      providers: [
        TwoFactorValidationFacade,
        { provide: AuthSessionService, useValue: authSessionMock },
        { provide: Router, useValue: routerMock },
        { provide: SnackbarHandler, useValue: snackbarMock },
      ],
    });
    return TestBed.inject(TwoFactorValidationFacade);
  };

  beforeEach(() => {
    authSessionMock = {
      completeTwoFactor: vi
        .fn()
        .mockReturnValue(of({ documentType: 'CI', documentNumber: '12345678' })),
    };
    routerMock = {
      getCurrentNavigation: vi.fn().mockReturnValue(null),
      navigateByUrl: vi.fn().mockResolvedValue(true),
    };
    snackbarMock = { success: vi.fn() };
  });

  it('redirects to login when no session id is provided', () => {
    setupHistory({});

    buildFacade();

    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/iniciar-sesion');
  });

  it('restores email and session from history state', () => {
    setupHistory({ email: 'john.doe@example.com', sessionId: 'abc-123' });

    const facade = buildFacade();

    expect(facade.email()).toBe('john.doe@example.com');
  });

  it('completes 2FA via auth session and navigates home on success', () => {
    setupHistory({
      email: 'a@b.com',
      sessionId: 'abc-123',
      documentType: 'CI',
      documentNumber: '12345678',
    });
    const facade = buildFacade();

    facade.verify('123456');

    expect(authSessionMock.completeTwoFactor).toHaveBeenCalledWith({
      sessionId: 'abc-123',
      code: '123456',
      documentType: 'CI',
      documentNumber: '12345678',
    });
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/inicio');
  });

  it('sets a friendly error message when verification fails', () => {
    setupHistory({ email: 'a@b.com', sessionId: 'abc-123' });
    authSessionMock.completeTwoFactor.mockReturnValue(throwError(() => new Error('boom')));
    const facade = buildFacade();

    facade.verify('111111');

    expect(facade.error()).toContain('No pudimos validar el código');
    expect(facade.isSubmitting()).toBe(false);
  });

  it('shows a snackbar when resend is requested', () => {
    setupHistory({ email: 'a@b.com', sessionId: 'abc-123' });
    const facade = buildFacade();

    facade.resend();

    expect(snackbarMock.success).toHaveBeenCalledWith('Te enviamos un nuevo código a tu correo.');
  });
});

