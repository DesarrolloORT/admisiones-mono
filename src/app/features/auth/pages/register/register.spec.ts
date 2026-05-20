import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../../catalogs/services/catalogs';
import { RegisterFlowFacade } from '../../facades/register-flow.facade';
import { Auth } from '../../services/auth';
import { DocumentRecognition } from '../../services/document-recognition';
import { Register } from './register';

describe('Register', () => {
  let fixture: ComponentFixture<Register>;
  let component: Register;
  let authMock: {
    evaluateDocument: ReturnType<typeof vi.fn>;
    verifyIdentity: ReturnType<typeof vi.fn>;
    confirmExistingPerson: ReturnType<typeof vi.fn>;
  };
  let catalogsMock: {
    getDocumentTypes: ReturnType<typeof vi.fn>;
    getCountryLocations: ReturnType<typeof vi.fn>;
    getCareers: ReturnType<typeof vi.fn>;
    getComienzos: ReturnType<typeof vi.fn>;
  };
  let documentRecognitionMock: {
    createRequestFromFile: ReturnType<typeof vi.fn>;
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authMock = {
      evaluateDocument: vi.fn().mockReturnValue(
        of({
          requiereAltaPersona: true,
          requiereAltaSolicitud: true,
          requiereVerificacion: true,
          solicitudAltaExistente: false,
          usuarioExistente: false,
        })
      ),
      verifyIdentity: vi.fn().mockReturnValue(of({ success: true })),
      confirmExistingPerson: vi.fn().mockReturnValue(of({ success: true })),
    };
    catalogsMock = {
      getDocumentTypes: vi.fn().mockReturnValue(
        of([
          { id: 1, label: 'Cédula de identidad', code: 'CI' },
          { id: 2, label: 'Pasaporte', code: 'PASS' },
          { id: 3, label: 'DNI', code: 'DNI' },
        ])
      ),
      getCountryLocations: vi.fn().mockReturnValue(
        of([
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
        ])
      ),
      getCareers: vi.fn().mockReturnValue(
        of([
          {
            idProducto: 20,
            idNivelProducto: 1,
            nombreProducto: 'Licenciatura en Diseño Gráfico',
            nombreNivelProducto: 'Carreras universitarias',
          },
        ])
      ),
      getComienzos: vi
        .fn()
        .mockReturnValue(of([{ idProceso: 30, nombreProceso: 'Marzo 2026. 08:00 - 14:00' }])),
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
              lugarNacimiento: 'Montevideo / URY',
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

  it('should continue from identity to personal step', async () => {
    const facade = component['facade'];
    facade.identityForm.setValue({
      documentType: 'CI',
      documentNumber: '12345678',
    });

    await facade.continueToPersonalData();

    expect(authMock.evaluateDocument).toHaveBeenCalledWith('CI', '12345678');
    expect(facade.step()).toBe('personal');
  });

  it('should send selected document to the API and preload returned fields', async () => {
    const facade = component['facade'];
    const file = new File(['binary-content'], 'cedula.pdf', { type: 'application/pdf' });
    const input = document.createElement('input');

    Object.defineProperty(input, 'files', {
      configurable: true,
      value: {
        item: (index: number) => (index === 0 ? file : null),
      },
    });

    await facade.onDocumentSelected({ target: input } as unknown as Event);

    expect(documentRecognitionMock.createRequestFromFile).toHaveBeenCalledWith(file);
    expect(documentRecognitionMock.recognizeDocument).toHaveBeenCalledWith({
      tipoMime: 'application/pdf',
      archivoAdjunto: {
        nombreArchivo: 'cedula.pdf',
        archivo: 'base64-content',
      },
    });
    expect(facade.selectedFileName()).toBe('cedula.pdf');
    expect(facade.identityForm.getRawValue()).toEqual({
      documentType: 'CI',
      documentNumber: '12345678',
    });
    expect(facade.personalForm.controls.primerNombre.value).toBe('Ana');
    expect(facade.personalForm.controls.primerApellido.value).toBe('Silva');
    expect(facade.personalForm.controls.location.value).toEqual({
      codigoPais: 1,
      codigoEstado: 10,
      codigoCiudad: null,
    });
    expect(facade.recognitionSuccessMessage()).toBe(
      'Datos precargados. Revisalos antes de continuar.'
    );
  });

  it('should verify identity and move to career step', () => {
    const facade = component['facade'];
    facade.identityForm.setValue({
      documentType: 'CI',
      documentNumber: '12345678',
    });
    setValidPersonalForm(facade);

    facade.submitPersonalData();

    expect(authMock.verifyIdentity).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '12345678',
      primerApellido: 'Silva',
      mail: 'ana@example.com',
      verificacionMail: 'ana@example.com',
    });
    expect(catalogsMock.getCareers).toHaveBeenCalled();
    expect(facade.step()).toBe('career');
  });

  it('should reject mismatched emails before submitting', () => {
    const facade = component['facade'];
    setValidPersonalForm(facade);
    facade.personalForm.patchValue({ verificacionMail: 'otra@example.com' });

    facade.submitPersonalData();

    expect(authMock.verifyIdentity).not.toHaveBeenCalled();
    expect(facade.error()).toBe('Los e-mails ingresados no coinciden.');
  });

  it('should confirm career data', () => {
    const facade = component['facade'];
    facade.identityForm.setValue({
      documentType: 'CI',
      documentNumber: '12345678',
    });
    facade.careerForm.setValue({
      propuestaAcademica: 1,
      carrera: 20,
      comienzo: 30,
    });

    facade.submitCareerData();

    expect(authMock.confirmExistingPerson).toHaveBeenCalledWith({
      tipoDocumento: 'CI',
      documento: '12345678',
      idProducto: 20,
      idProceso: 30,
    });
    expect(facade.successMessage()).toBe('Cuenta creada correctamente.');
  });
});

function setValidPersonalForm(facade: RegisterFlowFacade): void {
  facade.personalForm.setValue({
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
}
