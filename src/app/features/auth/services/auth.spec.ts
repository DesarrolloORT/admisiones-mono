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
      login: vi.fn().mockReturnValue(of({ token: 'token-123', expiresAt: '2026-05-11T18:00:00Z' })),
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
        expect(session.token).toBe('token-123');
        expect(auth.isAuthenticated()).toBe(true);
      });

    expect(endpointMock.login).toHaveBeenCalledWith({
      documentType: 'CI',
      documentNumber: '12345678',
      password: 'secret',
    });

    expect(window.localStorage.getItem(storageKeys.token)).toBe('token-123');
    expect(window.localStorage.getItem(storageKeys.session)).toContain('12345678');
  });

  it('should delegate register payload', () => {
    const payload = {
      identity: {
        documentType: 'CI',
        documentNumber: '12345678',
      },
      personal: {
        firstName: 'Ana',
        secondName: 'Maria',
        firstLastName: 'Silva',
        secondLastName: 'Pereira',
        birthDate: '2000-01-01',
        sex: 'F',
        country: 'Uruguay',
        address: 'Mercedes 1234',
        phone: '099123456',
        email: 'ana@example.com',
        confirmEmail: 'ana@example.com',
      },
    };

    auth.register(payload).subscribe(response => {
      expect(response.success).toBe(true);
    });

    expect(endpointMock.register).toHaveBeenCalledWith(payload);
  });
});
