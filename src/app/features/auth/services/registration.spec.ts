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
    confirmExistingPerson: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
    confirmApplicationRequest: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      evaluateDocument: vi.fn().mockReturnValue(of({ usuarioExistente: false })),
      verifyIdentity: vi.fn().mockReturnValue(of({ success: true })),
      confirmExistingPerson: vi.fn().mockReturnValue(of({ success: true })),
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
      tipoDocumento: 'CI',
      documento: '1234567-8',
    });
  });

  it('should verify identity with a formatted document', () => {
    service
      .verifyExistingPersonIdentity({
        identity: { documentType: 'CI', documentNumber: '12345678' },
        primerApellido: 'Silva',
        mail: 'ana@example.com',
      })
      .subscribe();

    expect(endpointMock.verifyIdentity).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '1234567-8',
      primerApellido: 'Silva',
      mail: 'ana@example.com',
    });
  });

  it('should confirm existing-person career interest', () => {
    service
      .confirmCareerInterest({
        flow: 'existing-person',
        identity: { documentType: 'CI', documentNumber: '12345678' },
        personal: null,
        selection: { idProducto: 20, idProceso: 30 },
      })
      .subscribe();

    expect(endpointMock.confirmExistingPerson).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '1234567-8',
      idProducto: 20,
      idProceso: 30,
    });
  });

  it('should confirm a new person with full registration payload', () => {
    service
      .confirmCareerInterest({
        flow: 'new-person',
        identity: { documentType: 'CI', documentNumber: '12345678' },
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
        selection: { idProducto: 20, idProceso: 30 },
      })
      .subscribe();

    expect(endpointMock.register).toHaveBeenCalledWith(
      expect.objectContaining({
        tipoDocumento: 'CI',
        documento: '1234567-8',
        primerNombre: 'Ana',
        idProducto: 20,
        idProceso: 30,
      })
    );
  });

  it('should confirm a non-CI application request', () => {
    service
      .confirmCareerInterest({
        flow: 'new-application',
        identity: { documentType: 'PS', documentNumber: 'AB123456' },
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
        selection: { idProducto: 20, idProceso: 30 },
      })
      .subscribe();

    expect(endpointMock.confirmApplicationRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        tipoDocumento: 'PS',
        documento: 'AB123456',
      })
    );
  });
});
