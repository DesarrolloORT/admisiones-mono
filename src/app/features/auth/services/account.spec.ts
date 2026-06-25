import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AccountEndpoint } from '../endpoints/account.endpoint';
import { AccountService } from './account';

describe('AccountService', () => {
  let service: AccountService;
  let endpoint: {
    getPersonalData: ReturnType<typeof vi.fn>;
    updatePersonalData: ReturnType<typeof vi.fn>;
    changePassword: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpoint = {
      getPersonalData: vi.fn().mockReturnValue(of({ firstName: 'Gabriela' })),
      updatePersonalData: vi.fn().mockReturnValue(of(true)),
      changePassword: vi.fn().mockReturnValue(of(undefined)),
    };

    TestBed.configureTestingModule({
      providers: [AccountService, { provide: AccountEndpoint, useValue: endpoint }],
    });

    service = TestBed.inject(AccountService);
  });

  it('should expose account personal data through the endpoint adapter', () => {
    service.getPersonalData().subscribe(result => {
      expect(result).toEqual({ firstName: 'Gabriela' });
    });

    expect(endpoint.getPersonalData).toHaveBeenCalledOnce();
  });

  it('should update personal data through the endpoint adapter', () => {
    const payload = {
      countryCode: 1,
      stateCode: 10,
      cityCode: null,
      address: 'Mercedes 1234',
      phone: '99123456',
      email: 'gabriela@example.com',
      emailVerification: 'gabriela@example.com',
    };

    service.updatePersonalData(payload).subscribe(result => {
      expect(result).toBe(true);
    });

    expect(endpoint.updatePersonalData).toHaveBeenCalledWith(payload);
  });

  it('should change password through the endpoint adapter', () => {
    const payload = {
      currentPassword: 'ActualPassword1!',
      password: 'NuevaPassword1!',
    };

    service.changePassword(payload).subscribe(result => {
      expect(result).toBeUndefined();
    });

    expect(endpoint.changePassword).toHaveBeenCalledWith(payload);
  });
});
