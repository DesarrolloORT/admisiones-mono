import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { vi } from 'vitest';

import { Inscripciones } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionSurveyIdentityFacade } from './inscription-survey-identity';

describe('InscripcionSurveyIdentityFacade', () => {
  const uploadIdentityDocument = vi.fn();
  const uploadIdentityPhoto = vi.fn();
  const onIdentityChanged = vi.fn();

  beforeEach(() => {
    uploadIdentityDocument.mockReset().mockReturnValue(of(true));
    uploadIdentityPhoto.mockReset().mockReturnValue(of(true));
    onIdentityChanged.mockReset();
  });

  it('stores the selected file and notifies the context', () => {
    const identity = createFacade();
    const invalid = new File(['x'], 'invalido.png', { type: 'image/png' });
    const valid = new File(['ok'], 'frente.png', { type: 'image/png' });

    identity.updateIdentityFile(
      'frente',
      fileEvent([
        { isValid: false, file: invalid },
        { isValid: true, file: valid },
      ])
    );

    expect(identity.identityFiles().frente).toBe(valid);
    expect(onIdentityChanged).toHaveBeenCalledOnce();
  });

  it('is complete only with a valid form and the three files', () => {
    const identity = createFacade();
    identity.updateIdentityFile('frente', fileEvent([{ isValid: true, file: file('f.png') }]));
    identity.updateIdentityFile('dorso', fileEvent([{ isValid: true, file: file('d.png') }]));

    expect(identity.isComplete()).toBe(false);

    identity.updateIdentityFile('selfie', fileEvent([{ isValid: true, file: file('s.png') }]));
    expect(identity.isComplete()).toBe(false);

    identity.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    expect(identity.isComplete()).toBe(true);
  });

  it('saves without uploads when nothing changed', async () => {
    const identity = createFacade();

    await expect(firstValueFrom(identity.saveIdentityChanges())).resolves.toBe(true);
    expect(uploadIdentityDocument).not.toHaveBeenCalled();
    expect(uploadIdentityPhoto).not.toHaveBeenCalled();
  });

  it('uploads only the selfie when only the selfie changed', async () => {
    const identity = createFacade();
    const selfie = file('selfie.png');

    identity.updateIdentityFile('selfie', fileEvent([{ isValid: true, file: selfie }]));

    await expect(firstValueFrom(identity.saveIdentityChanges())).resolves.toBe(true);
    expect(uploadIdentityPhoto).toHaveBeenCalledWith(selfie);
    expect(uploadIdentityDocument).not.toHaveBeenCalled();
  });

  function createFacade(): InscripcionSurveyIdentityFacade {
    TestBed.configureTestingModule({
      providers: [
        InscripcionFormsStore,
        InscripcionSurveyIdentityFacade,
        {
          provide: Inscripciones,
          useValue: {
            getIdentityPreload: vi
              .fn()
              .mockReturnValue(
                of({ frente: null, dorso: null, selfie: null, fechaVencimiento: null })
              ),
            uploadIdentityDocument,
            uploadIdentityPhoto,
          },
        },
      ],
    });
    const identity = TestBed.inject(InscripcionSurveyIdentityFacade);
    identity.initialize({
      isIdentitySectionActive: signal(false),
      surveyLoadError: signal(null),
      onIdentityChanged,
    });
    return identity;
  }

  function file(name: string): File {
    return new File(['data'], name, { type: 'image/png' });
  }

  function fileEvent(
    value: { isValid: boolean; file: File; isPreloaded?: boolean }[]
  ): Parameters<InscripcionSurveyIdentityFacade['updateIdentityFile']>[1] {
    return { value } as Parameters<InscripcionSurveyIdentityFacade['updateIdentityFile']>[1];
  }
});
