import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { CacheService } from '@desarrolloort/ngx-utils';
import { of, throwError } from 'rxjs';
import { storageKeys } from 'src/app/core/storage/keys';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { Auth } from './auth';

describe('Auth', () => {
  let auth: Auth;
  let endpointMock: {
    login: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
    confirmApplicationRequest: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
  };
  let routerMock: { navigateByUrl: ReturnType<typeof vi.fn> };
  let cacheMock: { clear: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    window.localStorage.clear();
    endpointMock = {
      login: vi.fn().mockReturnValue(of({ documento: '12345678' })),
      register: vi.fn().mockReturnValue(of({ success: true })),
      confirmApplicationRequest: vi.fn().mockReturnValue(of({ success: true })),
      logout: vi.fn().mockReturnValue(of(undefined)),
    };
    routerMock = { navigateByUrl: vi.fn() };
    cacheMock = { clear: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        Auth,
        { provide: AuthEndpoint, useValue: endpointMock },
        { provide: Router, useValue: routerMock },
        { provide: CacheService, useValue: cacheMock },
      ],
    });

    auth = TestBed.inject(Auth);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    window.localStorage.clear();
  });

  it('should delegate login and persist the returned session', () => {
    auth
      .login({
        documentType: 'CI',
        documentNumber: '12345678',
        password: 'secret',
      })
      .subscribe(session => {
        expect(session.documentNumber).toBe('12345678');
        expect(auth.isAuthenticated()).toBe(true);
      });

    expect(endpointMock.login).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '1234567-8',
      password: 'secret',
    });

    expect(window.localStorage.getItem(storageKeys.token)).toBeNull();
    expect(window.localStorage.getItem(storageKeys.session)).toContain('12345678');
  });

  it('should delegate register payload', () => {
    const payload = {
      identity: {
        documentType: 'CI',
        documentNumber: '12345678',
      },
      personal: {
        primerNombre: 'Ana',
        segundoNombre: 'Maria',
        primerApellido: 'Silva',
        segundoApellido: 'Pereira',
        fechaNacimiento: '2000-01-01',
        sexo: 'F',
        codigoPais: 1,
        codigoEstado: 10,
        codigoCiudad: 100,
        direccion: 'Mercedes 1234',
        telefono1: '099123456',
        mail: 'ana@example.com',
        verificacionMail: 'ana@example.com',
      },
    };

    auth.register(payload, 'flow-new-person').subscribe(response => {
      expect(response.success).toBe(true);
    });

    expect(endpointMock.register).toHaveBeenCalledWith(
      {
        tipoDocumento: 'CI',
        documento: '1234567-8',
        primerNombre: 'Ana',
        segundoNombre: 'Maria',
        primerApellido: 'Silva',
        segundoApellido: 'Pereira',
        fechaNacimiento: '2000-01-01',
        sexo: 'F',
        direccion: 'Mercedes 1234',
        telefono1: '099123456',
        mail: 'ana@example.com',
        verificacionMail: 'ana@example.com',
        codigoPais: 1,
        codigoEstado: 10,
        codigoCiudad: 100,
      },
      'flow-new-person'
    );
  });

  it('should delegate application request confirmation with formatted document', () => {
    const payload = {
      identity: {
        documentType: 'PS',
        documentNumber: 'AB123456',
      },
      personal: {
        primerNombre: 'Ana',
        segundoNombre: '',
        primerApellido: 'Silva',
        segundoApellido: '',
        fechaNacimiento: '2000-01-01',
        sexo: 'F',
        codigoPais: 1,
        codigoEstado: 10,
        codigoCiudad: 100,
        direccion: 'Mercedes 1234',
        telefono1: '099123456',
        mail: 'ana@example.com',
        verificacionMail: 'ana@example.com',
      },
    };

    auth.confirmApplicationRequest(payload, 'flow-new-application').subscribe(response => {
      expect(response.success).toBe(true);
    });

    expect(endpointMock.confirmApplicationRequest).toHaveBeenCalledWith(
      {
        tipoDocumento: 'PS',
        documento: 'AB123456',
        primerNombre: 'Ana',
        segundoNombre: null,
        primerApellido: 'Silva',
        segundoApellido: null,
        fechaNacimiento: '2000-01-01',
        sexo: 'F',
        direccion: 'Mercedes 1234',
        telefono1: '099123456',
        mail: 'ana@example.com',
        verificacionMail: 'ana@example.com',
        codigoPais: 1,
        codigoEstado: 10,
        codigoCiudad: 100,
      },
      'flow-new-application'
    );
  });

  describe('logout', () => {
    it('should call the logout endpoint, clear session state, storage, cache and navigate to login', () => {
      auth.login({ documentType: 'CI', documentNumber: '12345678', password: 'x' }).subscribe();

      expect(auth.isAuthenticated()).toBe(true);

      auth.logout();

      expect(endpointMock.logout).toHaveBeenCalled();
      expect(auth.isAuthenticated()).toBe(false);
      expect(window.localStorage.getItem(storageKeys.token)).toBeNull();
      expect(window.localStorage.getItem(storageKeys.session)).toBeNull();
      expect(cacheMock.clear).toHaveBeenCalled();
      expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/login');
    });

    it('should clear session locally even when the backend logout call fails', () => {
      auth.login({ documentType: 'CI', documentNumber: '12345678', password: 'x' }).subscribe();
      endpointMock.logout.mockReturnValue(throwError(() => new Error('network error')));

      auth.logout();

      expect(auth.isAuthenticated()).toBe(false);
      expect(window.localStorage.getItem(storageKeys.session)).toBeNull();
      expect(routerMock.navigateByUrl).toHaveBeenCalledWith('/login');
    });
  });
});
