import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthEndpoint } from '../endpoints/auth.endpoint';
import { RegistrationService } from './registration';

describe('RegistrationService', () => {
  let service: RegistrationService;
  let endpointMock: {
    evaluateDocument: ReturnType<typeof vi.fn>;
    verifyIdentity: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
    confirmApplicationRequest: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      evaluateDocument: vi.fn().mockReturnValue(of({ userExists: false })),
      verifyIdentity: vi.fn().mockReturnValue(of({ success: true })),
      register: vi.fn().mockReturnValue(of({ success: true })),
      confirmApplicationRequest: vi.fn().mockReturnValue(of({ success: true })),
    };

    TestBed.configureTestingModule({
      providers: [RegistrationService, { provide: AuthEndpoint, useValue: endpointMock }],
    });

    service = TestBed.inject(RegistrationService);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should evaluate a formatted document', () => {
    service.evaluateDocument({ documentType: 'CI', documentNumber: '12345678' }).subscribe();

    expect(endpointMock.evaluateDocument).toHaveBeenCalledWith({
      documentType: 'CI',
      documentNumber: '1234567-8',
    });
  });

  it('should verify identity with a formatted document', () => {
    service
      .verifyExistingPersonIdentity({
        flowId: 'flow-existing-person',
        identity: { documentType: 'CI', documentNumber: '12345678' },
        firstSurname: 'Silva',
        email: 'ana@example.com',
      })
      .subscribe();

    expect(endpointMock.verifyIdentity).toHaveBeenCalledWith(
      {
        documentType: 'CI',
        documentNumber: '1234567-8',
        firstSurname: 'Silva',
        email: 'ana@example.com',
      },
      'flow-existing-person'
    );
  });

  it('should confirm a new person with full registration payload', () => {
    service
      .confirmRegistration({
        flow: 'new-person',
        flowId: 'flow-new-person',
        identity: { documentType: 'CI', documentNumber: '12345678' },
        personal: {
          firstName: 'Ana',
          middleName: '',
          firstSurname: 'Silva',
          secondSurname: '',
          birthDate: '2000-01-01',
          sex: 'F',
          countryCode: 1,
          stateCode: 10,
          cityCode: 100,
          address: 'Mercedes 1234',
          primaryPhone: { nationalNumber: '099123456', iso2: 'UY' },
          email: 'ana@example.com',
          emailConfirmation: 'ana@example.com',
        },
      })
      .subscribe();

    expect(endpointMock.register).toHaveBeenCalledWith(
      expect.objectContaining({
        documentType: 'CI',
        documentNumber: '1234567-8',
        firstName: 'Ana',
      }),
      'flow-new-person'
    );
  });

  it('should confirm a non-CI application request', () => {
    service
      .confirmRegistration({
        flow: 'new-application',
        flowId: 'flow-new-application',
        identity: { documentType: 'PS', documentNumber: 'AB123456' },
        personal: {
          firstName: 'Ana',
          middleName: '',
          firstSurname: 'Silva',
          secondSurname: '',
          birthDate: '2000-01-01',
          sex: 'F',
          countryCode: 1,
          stateCode: 10,
          cityCode: 100,
          address: 'Mercedes 1234',
          primaryPhone: { nationalNumber: '099123456', iso2: 'UY' },
          email: 'ana@example.com',
          emailConfirmation: 'ana@example.com',
        },
      })
      .subscribe();

    expect(endpointMock.confirmApplicationRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        documentType: 'PS',
        documentNumber: 'AB123456',
      }),
      'flow-new-application'
    );
  });
});
