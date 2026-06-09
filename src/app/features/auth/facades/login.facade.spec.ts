import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { Catalogs } from '../../catalogs/services/catalogs';
import { AuthSessionService } from '../services/auth-session';
import { LoginFacade } from './login.facade';

describe('LoginFacade', () => {
  let facade: LoginFacade;
  let authMock: {
    login: ReturnType<typeof vi.fn>;
  };
  let routerMock: {
    navigateByUrl: ReturnType<typeof vi.fn>;
    navigate: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authMock = {
      login: vi.fn().mockReturnValue(
        of({
          kind: 'authenticated',
          session: {
            token: 'token-123',
            documentType: 'CI',
            documentNumber: '12345678',
            primerNombre: 'Ana',
            expiresAt: null,
          },
        })
      ),
    };
    routerMock = {
      navigateByUrl: vi.fn().mockResolvedValue(true),
      navigate: vi.fn().mockResolvedValue(true),
    };

    TestBed.configureTestingModule({
      providers: [
        LoginFacade,
        { provide: AuthSessionService, useValue: authMock },
        {
          provide: Catalogs,
          useValue: { getDocumentTypes: vi.fn().mockReturnValue(of([])) },
        },
        { provide: Router, useValue: routerMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: { get: () => null },
            },
          },
        },
        {
          provide: SnackbarHandler,
          useValue: { success: vi.fn(), error: vi.fn(), show: vi.fn() },
        },
      ],
    });

    facade = TestBed.inject(LoginFacade);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should submit valid credentials, clear password and navigate home', () => {
    facade.form.setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });

    facade.submit();

    expect(authMock.login).toHaveBeenCalledWith({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });
    expect(facade.form.controls.password.value).toBe('');
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/inicio');
  });

  it('should mark the form as touched when invalid', () => {
    facade.submit();

    expect(authMock.login).not.toHaveBeenCalled();
    expect(facade.form.touched).toBe(true);
  });

  it('should expose auth errors in UI state', () => {
    authMock.login.mockReturnValue(
      throwError(() => ({
        status: 401,
        message: 'Credenciales inválidas.',
        action: 'notify',
        isOperationResult: true,
        originalError: new Error('boom'),
      }))
    );
    facade.form.setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });

    facade.submit();

    expect(facade.error()).toBe('Credenciales inválidas.');
  });

  it('should navigate to /verificar-codigo with state when 2FA is required', () => {
    authMock.login.mockReturnValue(
      of({
        kind: 'twoFactorRequired',
        sessionId: 'ab4df653422a4c19be2867c08355fa27',
        maskedEmail: 'c******a@gmail.******',
        message: 'Se envió un código de verificación a tu correo electrónico.',
      })
    );
    facade.form.setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });

    facade.submit();

    expect(routerMock.navigate).toHaveBeenCalledWith(
      ['/verificar-codigo'],
      {
        state: {
          email: 'c******a@gmail.******',
          sessionId: 'ab4df653422a4c19be2867c08355fa27',
          documentType: 'CI',
          documentNumber: '11111111',
        },
      }
    );
    expect(routerMock.navigateByUrl).not.toHaveBeenCalled();
    expect(facade.form.controls.password.value).toBe('');
  });
});

