import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { AccountService } from './account';
import { AuthSessionService } from './auth-session';

describe('AuthSessionService', () => {
  let service: AuthSessionService;
  let endpointMock: {
    login: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
    clearCache: ReturnType<typeof vi.fn>;
    refreshToken: ReturnType<typeof vi.fn>;
    resendTwoFactorCode: ReturnType<typeof vi.fn>;
  };
  let routerMock: { navigateByUrl: ReturnType<typeof vi.fn> };
  let accountMock: { getPersonalData: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    endpointMock = {
      login: vi
        .fn()
        .mockReturnValue(of({ kind: 'authenticated', documento: '12345678', primerNombre: 'Ana' })),
      logout: vi.fn().mockReturnValue(of(undefined)),
      clearCache: vi.fn(),
      refreshToken: vi.fn().mockReturnValue(of(undefined)),
      resendTwoFactorCode: vi.fn().mockReturnValue(
        of({
          sessionId: 'session-456',
          maskedEmail: 'a***@example.com',
          message: 'Código reenviado.',
        })
      ),
    };
    routerMock = { navigateByUrl: vi.fn() };
    accountMock = {
      getPersonalData: vi.fn().mockReturnValue(
        of({
          documentType: 'CI',
          documentNumber: '12345678',
          firstName: 'Ana',
          secondName: '',
          firstLastName: 'Pérez',
          secondLastName: '',
          birthDate: '',
          sex: '',
          countryCode: null,
          stateCode: null,
          cityCode: null,
          address: '',
          phone: '',
          email: '',
          emailVerification: '',
        })
      ),
    };

    TestBed.configureTestingModule({
      providers: [
        AuthSessionService,
        { provide: AuthEndpoint, useValue: endpointMock },
        { provide: AccountService, useValue: accountMock },
        { provide: Router, useValue: routerMock },
      ],
    });

    service = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    window.localStorage.clear();
    window.sessionStorage.clear();
  });

  it('should delegate login and persist the returned session', () => {
    service
      .login({
        documentType: 'CI',
        documentNumber: '12345678',
        password: 'secret',
      })
      .subscribe(outcome => {
        expect(outcome.kind).toBe('authenticated');
        if (outcome.kind === 'authenticated') {
          expect(outcome.session.documentNumber).toBe('12345678');
          expect(outcome.session.primerNombre).toBe('Ana');
        }
        expect(service.isAuthenticated()).toBe(true);
      });

    expect(endpointMock.login).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '1234567-8',
      password: 'secret',
    });
    expect(window.localStorage.getItem('auth-session')).toBeNull();
  });

  it('should propagate twoFactorRequired outcome without storing a session', () => {
    endpointMock.login.mockReturnValue(
      of({
        kind: 'twoFactorRequired',
        sessionId: 'abc',
        maskedEmail: 'c***a@gmail.***',
        message: 'envio',
      })
    );

    service
      .login({ documentType: 'CI', documentNumber: '12345678', password: 'secret' })
      .subscribe(outcome => {
        expect(outcome.kind).toBe('twoFactorRequired');
        if (outcome.kind === 'twoFactorRequired') {
          expect(service.takePendingTwoFactorContext()).toEqual({
            documentNumber: '12345678',
            documentType: 'CI',
            email: 'c***a@gmail.***',
            sessionId: 'abc',
          });
        }
      });

    expect(service.isAuthenticated()).toBe(false);
    expect(window.localStorage.getItem('auth-session')).toBeNull();
  });

  it('should delegate resending the two-factor code', () => {
    service.resendTwoFactorCode('session-123').subscribe(result => {
      expect(result.sessionId).toBe('session-456');
    });

    expect(endpointMock.resendTwoFactorCode).toHaveBeenCalledWith({
      sessionId: 'session-123',
    });
  });

  it('should hydrate an authenticated cookie session in memory', () => {
    service.hydrateAuthenticatedSession().subscribe(session => {
      expect(session.documentNumber).toBe('12345678');
      expect(session.primerNombre).toBe('Ana');
    });

    expect(accountMock.getPersonalData).toHaveBeenCalled();
    expect(service.isAuthenticated()).toBe(true);
    expect(window.localStorage.getItem('auth-session')).toBeNull();
  });

  it('should validate an authenticated cookie session through personal data', () => {
    service.ensureAuthenticatedSession().subscribe(isAuthenticated => {
      expect(isAuthenticated).toBe(true);
    });

    expect(accountMock.getPersonalData).toHaveBeenCalled();
    expect(service.isAuthenticated()).toBe(true);
  });

  it('should clear stale local session when cookie validation fails', () => {
    service.login({ documentType: 'CI', documentNumber: '12345678', password: 'x' }).subscribe();
    accountMock.getPersonalData.mockReturnValue(throwError(() => new Error('unauthorized')));

    service.ensureAuthenticatedSession().subscribe(isAuthenticated => {
      expect(isAuthenticated).toBe(false);
    });

    expect(service.isAuthenticated()).toBe(false);
    expect(window.localStorage.getItem('auth-session')).toBeNull();
    expect(endpointMock.clearCache).toHaveBeenCalled();
  });

  it('should delegate access token refresh without changing local session', () => {
    service.refreshAccessToken().subscribe(result => {
      expect(result).toBeUndefined();
    });

    expect(endpointMock.refreshToken).toHaveBeenCalled();
  });

  it('should clear session locally after logout', () => {
    service.login({ documentType: 'CI', documentNumber: '12345678', password: 'x' }).subscribe();

    service.logout();

    expect(endpointMock.logout).toHaveBeenCalled();
    expect(service.isAuthenticated()).toBe(false);
    expect(window.localStorage.getItem('auth-session')).toBeNull();
    expect(endpointMock.clearCache).toHaveBeenCalled();
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/iniciar-sesion');
  });

  it('should clear session locally even when backend logout fails', () => {
    service.login({ documentType: 'CI', documentNumber: '12345678', password: 'x' }).subscribe();
    endpointMock.logout.mockReturnValue(throwError(() => new Error('network error')));

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(window.localStorage.getItem('auth-session')).toBeNull();
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/iniciar-sesion');
  });
});
