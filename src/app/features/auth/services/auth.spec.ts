import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { storageKeys } from 'src/app/core/storage/keys';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { Auth } from './auth';

describe('Auth', () => {
  let auth: Auth;
  let endpointMock: {
    login: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    window.localStorage.clear();
    endpointMock = {
      login: vi.fn().mockReturnValue(of({ documento: '12345678' })),
      register: vi.fn().mockReturnValue(of({ success: true })),
    };

    TestBed.configureTestingModule({
      providers: [Auth, { provide: AuthEndpoint, useValue: endpointMock }],
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
      codigoPersona: 12345678,
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

    auth.register(payload).subscribe(response => {
      expect(response.success).toBe(true);
    });

    expect(endpointMock.register).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '12345678',
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
    });
  });
});

