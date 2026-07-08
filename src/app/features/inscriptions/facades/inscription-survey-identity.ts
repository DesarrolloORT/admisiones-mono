import { computed, DestroyRef, effect, inject, Signal, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Validators } from '@angular/forms';
import type { OrtFileUploaderChange, OrtPreloadedFile } from '@desarrolloort/components';
import { forkJoin, Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

import type { ArchivosIdentidad } from '../models/inscription-flow';
import type {
  IdentityFileTarget,
  IdentityPreloadedFileMap,
} from '../models/inscription-flow-forms';
import { parseDate, serializeDate } from '../models/inscription-flow-mappers';
import { Inscripciones, type InscripcionIdentityPreload } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';

export interface SurveyIdentityContext {
  /** La sección de identidad está visible (paso encuesta + sección activa). */
  isIdentitySectionActive: Signal<boolean>;
  surveyLoadError: Signal<string | null>;
  /** Cambió el estado de identidad: re-sincronizar la completitud de la sección. */
  onIdentityChanged(): void;
}

/**
 * Verificación de identidad del paso 2: archivos (frente/dorso/selfie), precarga
 * desde Persona/Documento y Persona/Foto, y guardado de cambios al cerrar el paso.
 * La fachada principal registra el contexto vía `initialize()` (sin inyección mutua).
 */
export class InscripcionSurveyIdentityFacade {
  private readonly inscriptions = inject(Inscripciones);
  private readonly destroyRef = inject(DestroyRef);

  public readonly identityForm = inject(InscripcionFormsStore).identityForm;
  public readonly acceptedImageTypes = ['image/jpeg', 'image/png'];

  private readonly context = signal<SurveyIdentityContext | null>(null);
  private identityPreloadRequested = false;
  private initialIdentityExpiration = '';
  private readonly identityFileTouched = new Set<IdentityFileTarget>();

  public readonly identityFiles = signal<ArchivosIdentidad>({
    frente: null,
    dorso: null,
    selfie: null,
  });
  private readonly preloadedIdentityFiles = signal<IdentityPreloadedFileMap>({
    frente: null,
    dorso: null,
    selfie: null,
  });
  public readonly requiresIdentityConfirmation = signal(false);

  public readonly initialIdentityFiles = computed(() => {
    const files = this.preloadedIdentityFiles();
    return {
      frente: files.frente ? [files.frente] : [],
      dorso: files.dorso ? [files.dorso] : [],
      selfie: files.selfie ? [files.selfie] : [],
    };
  });

  constructor() {
    effect(() => {
      const context = this.context();
      if (
        !context ||
        this.identityPreloadRequested ||
        context.surveyLoadError() ||
        !context.isIdentitySectionActive()
      ) {
        return;
      }
      this.identityPreloadRequested = true;
      untracked(() => this.loadIdentityPreload());
    });
  }

  public initialize(context: SurveyIdentityContext): void {
    this.context.set(context);
  }

  public updateIdentityFile(target: IdentityFileTarget, event: OrtFileUploaderChange): void {
    const selected =
      event.value.find(file => file.isValid && !file.isPreloaded) ??
      event.value.find(file => file.isValid) ??
      null;
    const selectedFile = selected?.file ?? null;
    const currentFile = this.identityFiles()[target];

    if (
      selected?.isPreloaded &&
      currentFile &&
      selectedFile &&
      currentFile.name === selectedFile.name &&
      currentFile.size === selectedFile.size &&
      currentFile.type === selectedFile.type
    ) {
      return;
    }

    this.identityFileTouched.add(target);
    this.preloadedIdentityFiles.update(files => ({ ...files, [target]: null }));
    this.identityFiles.update(files => ({ ...files, [target]: selectedFile }));
    this.context()?.onIdentityChanged();
  }

  /** Form válido y los tres archivos presentes. */
  public isComplete(): boolean {
    const files = this.identityFiles();
    return (
      this.identityForm.valid &&
      files.frente !== null &&
      files.dorso !== null &&
      files.selfie !== null
    );
  }

  public saveIdentityChanges(): Observable<boolean> {
    const files = this.identityFiles();
    const expiration = serializeDate(this.identityForm.controls.vencimientoDocumento.value);
    const documentChanged =
      this.identityFileTouched.has('frente') ||
      this.identityFileTouched.has('dorso') ||
      expiration !== this.initialIdentityExpiration;
    const uploads: Observable<boolean>[] = [];

    if (expiration && files.frente && files.dorso && documentChanged) {
      uploads.push(
        this.inscriptions.uploadIdentityDocument({
          fecha: expiration,
          frente: files.frente,
          dorso: files.dorso,
        })
      );
    }

    if (files.selfie && this.identityFileTouched.has('selfie')) {
      uploads.push(this.inscriptions.uploadIdentityPhoto(files.selfie));
    }

    return uploads.length === 0
      ? of(true)
      : forkJoin(uploads).pipe(map(results => results.every(Boolean)));
  }

  private loadIdentityPreload(): void {
    this.inscriptions
      .getIdentityPreload()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: preload => this.applyIdentityPreload(preload),
        error: () => undefined,
      });
  }

  private setIdentityConfirmationRequired(required: boolean): void {
    this.requiresIdentityConfirmation.set(required);
    const control = this.identityForm.controls.identidadCorrecta;
    control.setValidators(required ? Validators.requiredTrue : null);
    if (!required) control.setValue(false, { emitEvent: false });
    control.updateValueAndValidity({ emitEvent: false });
  }

  private applyIdentityPreload(preload: InscripcionIdentityPreload): void {
    const expiration = parseDate(preload.fechaVencimiento);
    this.initialIdentityExpiration = serializeDate(expiration);
    this.setIdentityConfirmationRequired(
      !!preload.frente && !!preload.dorso && !!preload.selfie && !!expiration
    );

    this.applyPreloadedIdentityFile('frente', preload.frente);
    this.applyPreloadedIdentityFile('dorso', preload.dorso);
    this.applyPreloadedIdentityFile('selfie', preload.selfie);

    const expirationControl = this.identityForm.controls.vencimientoDocumento;
    if (expiration && !expirationControl.value && !expirationControl.dirty) {
      expirationControl.setValue(expiration);
    }
    this.context()?.onIdentityChanged();
  }

  private applyPreloadedIdentityFile(target: IdentityFileTarget, file: File | null): void {
    if (!file || this.identityFileTouched.has(target) || this.identityFiles()[target]) return;
    this.identityFiles.update(files => ({ ...files, [target]: file }));
    void file
      .arrayBuffer()
      .then(src => {
        if (this.identityFileTouched.has(target) || this.identityFiles()[target] !== file) return;
        this.preloadedIdentityFiles.update(files => ({
          ...files,
          [target]: this.toIdentityPreloadedFile(target, file, src),
        }));
      })
      .catch(() => undefined);
  }

  private toIdentityPreloadedFile(
    target: IdentityFileTarget,
    file: File,
    src: ArrayBuffer
  ): OrtPreloadedFile {
    return {
      id: `identity-preload-${target}`,
      name: file.name,
      size: file.size,
      type: file.type,
      src,
    };
  }
}
