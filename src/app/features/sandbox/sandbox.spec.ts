import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Sandbox } from './sandbox';
import { SandboxAuth } from './services/sandbox-auth';

describe('Sandbox', () => {
  let fixture: ComponentFixture<Sandbox>;
  let component: Sandbox;
  let authMock: {
    loginUrl: string;
    documentRecognitionUrl: string;
    login: ReturnType<typeof vi.fn>;
    recognizeDocument: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authMock = {
      loginUrl: '/ORT/Login',
      documentRecognitionUrl: '/ReconocimientoDocumento/Reconocer',
      login: vi.fn().mockReturnValue(
        of({
          message: 'Autenticación exitosa. Los tokens han sido establecidos como cookies seguras.',
        })
      ),
      recognizeDocument: vi.fn().mockReturnValue(
        of({
          success: true,
          data: {
            requiereRevision: true,
            campos: {
              nombres: 'Juan',
              numeroDocumento: '1234567',
            },
          },
        })
      ),
    };

    TestBed.configureTestingModule({
      imports: [Sandbox],
      providers: [
        {
          provide: SandboxAuth,
          useValue: authMock,
        },
      ],
    });

    fixture = TestBed.createComponent(Sandbox);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should create the feature', () => {
    expect(component).toBeTruthy();
  });

  it('should submit codigoPersona and password', () => {
    component['form'].setValue({
      codigoPersona: 123,
      password: 'secret-123',
    });

    component['onSubmit']();

    expect(authMock.login).toHaveBeenCalledWith({
      codigoPersona: 123,
      password: 'secret-123',
    });
    expect(component['isAuthenticated']()).toBe(true);
    expect(component['successMessage']()).toBe(
      'Autenticación exitosa. Los tokens han sido establecidos como cookies seguras.'
    );
  });

  it('should return to login view when user clicks change user', () => {
    component['form'].setValue({
      codigoPersona: 123,
      password: 'secret-123',
    });

    component['onSubmit']();
    component['onBackToLogin']();

    expect(component['isAuthenticated']()).toBe(false);
    expect(component['successMessage']()).toBeNull();
  });

  it('should not submit when the form is invalid', () => {
    component['onSubmit']();

    expect(authMock.login).not.toHaveBeenCalled();
  });

  it('should submit document recognition payload after authentication', async () => {
    component['isAuthenticated'].set(true);

    const file = new File(['binary-content'], 'cedula.pdf', { type: 'application/pdf' });

    component['recognitionForm'].setValue({
      tipoDocumentoEsperado: 'CI',
      tipoMime: 'application/pdf',
      archivo: file,
    });

    vi.spyOn(
      component as unknown as { readFileAsBase64: () => Promise<string> },
      'readFileAsBase64'
    ).mockResolvedValue('base64-content');

    await component['onRecognizeDocument']();

    expect(authMock.recognizeDocument).toHaveBeenCalledWith({
      tipoDocumentoEsperado: 'CI',
      tipoMime: 'application/pdf',
      archivoAdjunto: {
        nombreArchivo: 'cedula.pdf',
        archivo: 'base64-content',
      },
    });
    expect(component['recognitionResponse']()?.success).toBe(true);
    expect(component['requiresReview']()).toBe(true);
    expect(component['detectedFields']()).toEqual([
      {
        key: 'nombres',
        label: 'Nombres',
        value: 'Juan',
      },
      {
        key: 'numeroDocumento',
        label: 'Numero Documento',
        value: '1234567',
      },
    ]);
  });

  it('should infer mime and expected document type from selected file name', () => {
    const file = new File(['binary-content'], 'pasaporte-frente.jpg', { type: '' });
    const input = document.createElement('input');

    Object.defineProperty(input, 'files', {
      configurable: true,
      value: {
        item: (index: number) => (index === 0 ? file : null),
      },
    });

    component['onDocumentSelected']({ target: input } as unknown as Event);

    expect(component['recognitionForm'].controls.tipoMime.value).toBe('image/jpeg');
    expect(component['recognitionForm'].controls.tipoDocumentoEsperado.value).toBe('PASAPORTE');
  });
});

