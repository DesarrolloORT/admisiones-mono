import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { PasswordActivationService } from './password-activation';

describe('PasswordActivationService', () => {
  let service: PasswordActivationService;
  let endpointMock: {
    activatePasswordLink: ReturnType<typeof vi.fn>;
    completePassword: ReturnType<typeof vi.fn>;
    recoverPassword: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      activatePasswordLink: vi.fn().mockReturnValue(of(undefined)),
      completePassword: vi.fn().mockReturnValue(of(undefined)),
      recoverPassword: vi.fn().mockReturnValue(of(undefined)),
    };

    TestBed.configureTestingModule({
      providers: [PasswordActivationService, { provide: AuthEndpoint, useValue: endpointMock }],
    });

    service = TestBed.inject(PasswordActivationService);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should activate a password link through the endpoint adapter', () => {
    service.activateLink('token-123').subscribe();

    expect(endpointMock.activatePasswordLink).toHaveBeenCalledWith({ token: 'token-123' });
  });

  it('should complete the password flow through the endpoint adapter', () => {
    service.completePassword('NuevaPassword1!').subscribe();

    expect(endpointMock.completePassword).toHaveBeenCalledWith({
      passwordNueva: 'NuevaPassword1!',
    });
  });

  it('should initiate password recovery through the endpoint adapter', () => {
    let completed = false;

    service
      .recoverPassword({ tipoDocumento: 'CI', documento: '1234567-8', primerApellido: 'Silva' })
      .subscribe({
        complete: () => {
          completed = true;
        },
      });

    expect(endpointMock.recoverPassword).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '1234567-8',
      primerApellido: 'Silva',
    });
    expect(completed).toBe(true);
  });

  it('should propagate password recovery failures', () => {
    const failure = new Error('recovery failed');
    endpointMock.recoverPassword.mockReturnValue(throwError(() => failure));
    let caught: unknown;

    service
      .recoverPassword({ tipoDocumento: 'CI', documento: '1234567-8', primerApellido: 'Silva' })
      .subscribe({
        error: error => {
          caught = error;
        },
      });

    expect(caught).toBe(failure);
  });
});
