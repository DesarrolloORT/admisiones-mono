import { TestBed } from '@angular/core/testing';

import { RegisterDocumentStore } from './register-document.store';

describe('RegisterDocumentStore', () => {
  let store: RegisterDocumentStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RegisterDocumentStore],
    });

    store = TestBed.inject(RegisterDocumentStore);
  });

  it('should keep selected file and document recognition data', () => {
    const file = new File(['content'], 'documento.pdf', { type: 'application/pdf' });

    store.setSelectedFile(file);
    store.setRecognitionResponse({
      data: {
        requiereRevision: true,
        campos: {
          tipoDocumento: 'CI',
          numeroDocumento: '12345678',
        },
        caraPersona: {
          nombreArchivo: 'face.png',
          contentType: 'image/png',
          archivo: 'face-base64',
        },
      },
    });

    expect(store.selectedFileName()).toBe('documento.pdf');
    expect(store.requiresReview()).toBe(true);
    expect(store.fields()?.numeroDocumento).toBe('12345678');
    expect(store.documentFace()?.archivo).toBe('face-base64');
  });
});
