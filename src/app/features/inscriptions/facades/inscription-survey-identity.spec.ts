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

  it('resolves to false when one of the uploads fails', async () => {
    const identity = createFacade();
    uploadIdentityDocument.mockReturnValue(of(false));
    identity.updateIdentityFile('frente', fileEvent([{ isValid: true, file: file('f.png') }]));
    identity.updateIdentityFile('dorso', fileEvent([{ isValid: true, file: file('d.png') }]));
    identity.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));

    await expect(firstValueFrom(identity.saveIdentityChanges())).resolves.toBe(false);
  });

  it('uploads the document when only the expiration date changes', async () => {
    const identity = createFacade();
    identity.updateIdentityFile('frente', fileEvent([{ isValid: true, file: file('f.png') }]));
    identity.updateIdentityFile('dorso', fileEvent([{ isValid: true, file: file('d.png') }]));
    await firstValueFrom(identity.saveIdentityChanges());
    uploadIdentityDocument.mockClear();

    identity.identityForm.controls.vencimientoDocumento.setValue(new Date(2031, 3, 15));

    await expect(firstValueFrom(identity.saveIdentityChanges())).resolves.toBe(true);
    expect(uploadIdentityDocument).toHaveBeenCalledWith({
      fecha: '2031-04-15',
      frente: identity.identityFiles().frente,
      dorso: identity.identityFiles().dorso,
    });
  });

  it('requires identity confirmation only when all three files and expiration preload', () => {
    const bytes = new Uint8Array([1, 2, 3]).buffer;
    const preloadedFile = (name: string): File =>
      ({
        name,
        size: 3,
        type: 'image/png',
        arrayBuffer: () => Promise.resolve(bytes),
      }) as File;
    const getIdentityPreload = vi.fn().mockReturnValue(
      of({
        frente: preloadedFile('frente.png'),
        dorso: preloadedFile('dorso.png'),
        selfie: null,
        fechaVencimiento: '2030-02-04',
      })
    );
    const identity = createFacade(getIdentityPreload, true);
    TestBed.tick();

    expect(identity.requiresIdentityConfirmation()).toBe(false);
    expect(identity.identityFiles().frente?.name).toBe('frente.png');
    expect(identity.identityFiles().selfie).toBeNull();
  });

  it('requires identity confirmation when the three files and expiration are preloaded', () => {
    const bytes = new Uint8Array([1, 2, 3]).buffer;
    const preloadedFile = (name: string): File =>
      ({
        name,
        size: 3,
        type: 'image/png',
        arrayBuffer: () => Promise.resolve(bytes),
      }) as File;
    const getIdentityPreload = vi.fn().mockReturnValue(
      of({
        frente: preloadedFile('frente.png'),
        dorso: preloadedFile('dorso.png'),
        selfie: preloadedFile('selfie.png'),
        fechaVencimiento: '2030-02-04',
      })
    );
    const identity = createFacade(getIdentityPreload, true);
    TestBed.tick();

    expect(identity.requiresIdentityConfirmation()).toBe(true);
    expect(identity.identityForm.controls.vencimientoDocumento.value).toEqual(new Date(2030, 1, 4));
  });

  it('maps preloaded files into the uploader preload list with array buffer contents', async () => {
    const bytes = new Uint8Array([9, 9, 9]).buffer;
    const preloadedFile = {
      name: 'frente.png',
      size: 3,
      type: 'image/png',
      arrayBuffer: () => Promise.resolve(bytes),
    } as File;
    const getIdentityPreload = vi.fn().mockReturnValue(
      of({
        frente: preloadedFile,
        dorso: null,
        selfie: null,
        fechaVencimiento: null,
      })
    );
    const identity = createFacade(getIdentityPreload, true);
    TestBed.tick();

    await Promise.resolve();
    await Promise.resolve();

    expect(identity.initialIdentityFiles().frente).toEqual([
      {
        id: 'identity-preload-frente',
        name: 'frente.png',
        size: 3,
        type: 'image/png',
        src: bytes,
      },
    ]);
    expect(identity.initialIdentityFiles().dorso).toEqual([]);
  });

  function createFacade(
    getIdentityPreload: ReturnType<typeof vi.fn> = vi
      .fn()
      .mockReturnValue(of({ frente: null, dorso: null, selfie: null, fechaVencimiento: null })),
    isIdentitySectionActive = false
  ): InscripcionSurveyIdentityFacade {
    TestBed.configureTestingModule({
      providers: [
        InscripcionFormsStore,
        InscripcionSurveyIdentityFacade,
        {
          provide: Inscripciones,
          useValue: {
            getIdentityPreload,
            uploadIdentityDocument,
            uploadIdentityPhoto,
          },
        },
      ],
    });
    const identity = TestBed.inject(InscripcionSurveyIdentityFacade);
    identity.initialize({
      isIdentitySectionActive: signal(isIdentitySectionActive),
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
