import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Auth } from '../../services/auth';
import { DocumentRecognition } from '../../services/document-recognition';
import { Register } from './register';

describe('Register', () => {
  let fixture: ComponentFixture<Register>;
  let component: Register;
  let authMock: {
    register: ReturnType<typeof vi.fn>;
  };
  let documentRecognitionMock: {
    createRequestFromFile: ReturnType<typeof vi.fn>;
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authMock = {
      register: vi.fn().mockReturnValue(of({ success: true, message: 'Registro enviado.' })),
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
    expect(component['personalForm'].controls.firstName.value).toBe('Ana');
    expect(component['personalForm'].controls.firstLastName.value).toBe('Silva');
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
    });

    component['submitPersonalData']();

    expect(authMock.register).toHaveBeenCalledWith({
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
    });
    expect(component['successMessage']()).toBe('Registro enviado.');
  });

  it('should reject mismatched emails before submitting', () => {
    component['personalForm'].setValue({
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
      confirmEmail: 'otra@example.com',
    });

    component['submitPersonalData']();

    expect(authMock.register).not.toHaveBeenCalled();
    expect(component['error']()).toBe('Los e-mails ingresados no coinciden.');
  });
});
