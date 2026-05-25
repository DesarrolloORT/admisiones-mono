import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { isNormalizedApiError } from '@desarrolloort/ngx-utils';
import { finalize } from 'rxjs/operators';

import { SnackbarHandler } from '../../../shared/ui/snackbar/snackbar-handler';
import { createRecoverAccessForm, syncDocumentNumberValidators } from '../forms/auth-forms';
import { formatDocumentForBackend, isCedulaDocumentType } from '../models/document-number';
import { PasswordActivationService } from '../services/password-activation';

@Injectable()
export class RecoverAccessFacade {
  private readonly passwordService = inject(PasswordActivationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarHandler);

  public readonly form = createRecoverAccessForm();
  public readonly isSubmitting = signal(false);
  public readonly error = signal<string | null>(null);
  public readonly successMessage = signal<string | null>(null);

  private readonly _documentTypeValue = toSignal(this.form.controls.documentType.valueChanges, {
    initialValue: this.form.controls.documentType.value,
  });

  public readonly isCedulaInput = computed(() => isCedulaDocumentType(this._documentTypeValue()));

  constructor() {
    effect(() => {
      syncDocumentNumberValidators(this.form.controls.documentNumber, this._documentTypeValue());
    });

    this.prefillFromQueryParams();
  }

  private prefillFromQueryParams(): void {
    const params = this.route.snapshot.queryParamMap;
    const tipoDoc = params.get('tipoDoc');
    const doc = params.get('doc');

    if (tipoDoc) {
      this.form.controls.documentType.setValue(tipoDoc);
    }
    if (doc) {
      this.form.controls.documentNumber.setValue(doc);
    }
  }

  public submit(): void {
    syncDocumentNumberValidators(this.form.controls.documentNumber, this._documentTypeValue());

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    const { documentType, documentNumber, primerApellido } = this.form.getRawValue();

    this.passwordService
      .recoverPassword({
        tipoDocumento: documentType,
        documento: formatDocumentForBackend(documentType, documentNumber),
        primerApellido,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          const message =
            'Si los datos coinciden, te enviaremos un correo con un link para recuperar tu acceso.';
          this.successMessage.set(message);
          this.snackbar.success(message);
        },
        error: error => {
          const message = this.getApiErrorMessage(
            error,
            'No se pudo procesar la solicitud. Intentá nuevamente.'
          );
          this.error.set(message);
          this.snackbar.error(message);
        },
      });
  }

  public goToLogin(): void {
    void this.router.navigate(['/login']);
  }

  private getApiErrorMessage(error: unknown, fallback: string): string {
    return isNormalizedApiError(error) ? error.message : fallback;
  }
}

