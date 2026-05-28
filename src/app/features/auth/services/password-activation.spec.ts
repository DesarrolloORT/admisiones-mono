import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { PasswordActivationService } from './password-activation';

describe('PasswordActivationService', () => {
  let service: PasswordActivationService;
  let endpointMock: {
    activatePasswordLink: ReturnType<typeof vi.fn>;
    completePassword: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      activatePasswordLink: vi.fn().mockReturnValue(of(undefined)),
      completePassword: vi.fn().mockReturnValue(of(undefined)),
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
});
