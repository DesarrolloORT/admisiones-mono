import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client.service';

import { Catalogs } from '../../../catalogs/services/catalogs';
import { Auth } from '../../services/auth';
import { DocumentRecognition } from '../../services/document-recognition';
import { Register } from './register';

describe('Register', () => {
  let fixture: ComponentFixture<Register>;
  let component: Register;
  let authMock: {
    register: ReturnType<typeof vi.fn>;
  };
  let catalogsMock: {
    getDocumentTypes: ReturnType<typeof vi.fn>;
  };
  let apiHttpClientMock: {
    request: ReturnType<typeof vi.fn>;
  };
  let documentRecognitionMock: {
    createRequestFromFile: ReturnType<typeof vi.fn>;
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authMock = {
      register: vi.fn().mockReturnValue(of({ success: true })),
    };
    catalogsMock = {
      getDocumentTypes: vi.fn().mockReturnValue(
        of([
          { id: 1, label: 'Cédula de identidad', code: 'CI' },
          { id: 2, label: 'Pasaporte', code: 'PASS' },
          { id: 3, label: 'DNI', code: 'DNI' },
        ])
      ),
    };
    apiHttpClientMock = {
      request: vi.fn().mockReturnValue(
        of({
          success: true,
          data: [
            {
              codigoPais: 1,
              nombre: 'Uruguay',
              estado: [
                {
                  codigoPais: 1,
                  codigoEstado: 10,
                  nombre: 'Montevideo',
                  ciudad: [
                    { codigoPais: 1, codigoEstado: 10, codigoCiudad: 100, nombre: 'Montevideo' },
                  ],
                },
              ],
            },
          ],
        })
      ),
    };
    documentRecognitionMock = {
      createRequestFromFile: vi.fn().mockResolvedValue({
        tipoMime: 'application/pdf',
        archivoAdjunto: {
          nombreArchivo: 'cedula.pdf',
          archivo: 'base64-content',
        },
      }),
      recognizeDocument: vi.fn().mockReturnValue(
        of({
          success: true,
          data: {
            requiereRevision: false,
            campos: {
              tipoDocumento: 'CI',
              numeroDocumento: '12345678',
              primerNombre: 'Ana',
              segundoNombre: 'Maria',
              primerApellido: 'Silva',
              segundoApellido: 'Pereira',
              fechaNacimiento: '2000-01-01',
              sexo: 'F',
              nacionalidad: 'Uruguay',
            },
            caraPersona: {
              nombreArchivo: 'cara.png',
              contentType: 'image/png',
              archivo: 'face-base64',
            },
          },
        })
      ),
    };

    TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        provideRouter([]),
        { provide: Auth, useValue: authMock },
        { provide: ApiHttpClient, useValue: apiHttpClientMock },
        { provide: Catalogs, useValue: catalogsMock },
        { provide: DocumentRecognition, useValue: documentRecognitionMock },
      ],
    });

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should create the register page', () => {
    expect(component).toBeTruthy();
  });

  it('should continue from identity to personal step', () => {
    component['identityForm'].setValue({
      documentType: 'CI',
      documentNumber: '12345678',
    });

    component['continueToPersonalData']();

    expect(component['step']()).toBe('personal');
  });

  it('should send selected document to the API and preload returned fields', async () => {
    const file = new File(['binary-content'], 'cedula.pdf', { type: 'application/pdf' });
    const input = document.createElement('input');

    Object.defineProperty(input, 'files', {
      configurable: true,
      value: {
        item: (index: number) => (index === 0 ? file : null),
      },
    });

    await component['onDocumentSelected']({ target: input } as unknown as Event);

    expect(documentRecognitionMock.createRequestFromFile).toHaveBeenCalledWith(file);
    expect(documentRecognitionMock.recognizeDocument).toHaveBeenCalledWith({
      tipoMime: 'application/pdf',
      archivoAdjunto: {
        nombreArchivo: 'cedula.pdf',
        archivo: 'base64-content',
      },
    });
    expect(component['selectedFileName']()).toBe('cedula.pdf');
    expect(component['identityForm'].getRawValue()).toEqual({
      documentType: 'CI',
      documentNumber: '12345678',
    });
    expect(component['personalForm'].controls.primerNombre.value).toBe('Ana');
    expect(component['personalForm'].controls.primerApellido.value).toBe('Silva');
    expect(component['recognitionSuccessMessage']()).toBe(
      'Datos precargados. Revisalos antes de continuar.'
    );
  });

  it('should submit personal registration data', () => {
    component['identityForm'].setValue({
      documentType: 'CI',
      documentNumber: '12345678',
    });
    component['personalForm'].setValue({
      primerNombre: 'Ana',
      segundoNombre: 'Maria',
      primerApellido: 'Silva',
      segundoApellido: 'Pereira',
      fechaNacimiento: '2000-01-01',
      sexo: 'F',
      location: { codigoPais: 1, codigoEstado: 10, codigoCiudad: 100 },
      direccion: 'Mercedes 1234',
      telefono1: '099123456',
      mail: 'ana@example.com',
      verificacionMail: 'ana@example.com',
    });

    component['submitPersonalData']();

    expect(authMock.register).toHaveBeenCalledWith({
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
    });
    expect(component['successMessage']()).toBe('Registro enviado correctamente.');
  });

  it('should reject mismatched emails before submitting', () => {
    component['personalForm'].setValue({
      primerNombre: 'Ana',
      segundoNombre: 'Maria',
      primerApellido: 'Silva',
      segundoApellido: 'Pereira',
      fechaNacimiento: '2000-01-01',
      sexo: 'F',
      location: { codigoPais: 1, codigoEstado: 10, codigoCiudad: 100 },
      direccion: 'Mercedes 1234',
      telefono1: '099123456',
      mail: 'ana@example.com',
      verificacionMail: 'otra@example.com',
    });

    component['submitPersonalData']();

    expect(authMock.register).not.toHaveBeenCalled();
    expect(component['error']()).toBe('Los e-mails ingresados no coinciden.');
  });
});

