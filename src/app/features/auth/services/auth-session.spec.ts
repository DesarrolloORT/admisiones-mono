import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { CacheService } from '@desarrolloort/ngx-utils';
import { of, throwError } from 'rxjs';
import { storageKeys } from 'src/app/core/storage/keys';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { AuthSessionService } from './auth-session';

describe('AuthSessionService', () => {
  let service: AuthSessionService;
  let endpointMock: {
    login: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
  };
  let routerMock: { navigateByUrl: ReturnType<typeof vi.fn> };
  let cacheMock: { clear: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    window.localStorage.clear();
    endpointMock = {
      login: vi.fn().mockReturnValue(of({ documento: '12345678', primerNombre: 'Ana' })),
      logout: vi.fn().mockReturnValue(of(undefined)),
    };
    routerMock = { navigateByUrl: vi.fn() };
    cacheMock = { clear: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        AuthSessionService,
        { provide: AuthEndpoint, useValue: endpointMock },
        { provide: Router, useValue: routerMock },
        { provide: CacheService, useValue: cacheMock },
      ],
    });

    service = TestBed.inject(AuthSessionService);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    window.localStorage.clear();
  });

  it('should delegate login and persist the returned session', () => {
    service
      .login({
        documentType: 'CI',
        documentNumber: '12345678',
        password: 'secret',
      })
      .subscribe(session => {
        expect(session.documentNumber).toBe('12345678');
        expect(session.primerNombre).toBe('Ana');
        expect(service.isAuthenticated()).toBe(true);
      });

    expect(endpointMock.login).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '1234567-8',
      password: 'secret',
    });
    expect(window.localStorage.getItem(storageKeys.token)).toBeNull();
    expect(window.localStorage.getItem(storageKeys.session)).toContain('12345678');
  });

  it('should clear session locally after logout', () => {
    service.login({ documentType: 'CI', documentNumber: '12345678', password: 'x' }).subscribe();

    service.logout();

    expect(endpointMock.logout).toHaveBeenCalled();
    expect(service.isAuthenticated()).toBe(false);
    expect(window.localStorage.getItem(storageKeys.session)).toBeNull();
    expect(cacheMock.clear).toHaveBeenCalled();
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/iniciar-sesion');
  });

  it('should clear session locally even when backend logout fails', () => {
    service.login({ documentType: 'CI', documentNumber: '12345678', password: 'x' }).subscribe();
    endpointMock.logout.mockReturnValue(throwError(() => new Error('network error')));

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(window.localStorage.getItem(storageKeys.session)).toBeNull();
    expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/iniciar-sesion');
  });
});

