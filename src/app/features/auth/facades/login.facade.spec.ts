import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../catalogs/services/catalogs';
import { AuthRequestError } from '../models/auth-error';
import { Auth } from '../services/auth';
import { LoginFacade } from './login.facade';

describe('LoginFacade', () => {
  let facade: LoginFacade;
  let authMock: {
    login: ReturnType<typeof vi.fn>;
  };
  let routerMock: {
    navigateByUrl: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authMock = {
      login: vi.fn().mockReturnValue(
        of({
          token: 'token-123',
          documentType: 'CI',
          documentNumber: '12345678',
          expiresAt: null,
        })
      ),
    };
    routerMock = {
      navigateByUrl: vi.fn().mockResolvedValue(true),
    };

    TestBed.configureTestingModule({
      providers: [
        LoginFacade,
        { provide: Auth, useValue: authMock },
        {
          provide: Catalogs,
          useValue: { getDocumentTypes: vi.fn().mockReturnValue(of([])) },
        },
        { provide: Router, useValue: routerMock },
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
      documentNumber: '12345678',
      password: 'secret',
    });

    facade.submit();

    expect(authMock.login).toHaveBeenCalledWith({
      documentType: 'CI',
      documentNumber: '12345678',
      password: 'secret',
    });
    expect(facade.form.controls.password.value).toBe('');
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/home');
  });

  it('should mark the form as touched when invalid', () => {
    facade.submit();

    expect(authMock.login).not.toHaveBeenCalled();
    expect(facade.form.touched).toBe(true);
  });

  it('should expose auth errors in UI state', () => {
    authMock.login.mockReturnValue(throwError(() => new AuthRequestError('login', 500)));
    facade.form.setValue({
      documentType: 'CI',
      documentNumber: '12345678',
      password: 'secret',
    });

    facade.submit();

    expect(facade.error()).toBe('No se pudo iniciar sesión. Error 500.');
  });
});
