import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';

import { Catalogs } from '../../catalogs/services/catalogs';
import {
  CEDULA_DOCUMENT_NUMBER_VALIDATORS,
  createLoginForm,
  NON_CEDULA_DOCUMENT_NUMBER_VALIDATORS,
} from '../forms/auth-forms';
import { AuthRequestError } from '../models/auth-error';
import { Auth } from '../services/auth';

@Injectable({
  providedIn: 'root',
})
export class LoginFacade {
  private readonly auth = inject(Auth);
  private readonly catalogs = inject(Catalogs);
  private readonly router = inject(Router);

  public readonly documentTypes$ = this.catalogs.getDocumentTypes();
  public readonly form = createLoginForm();
  public readonly isSubmitting = signal(false);
  public readonly showPassword = signal(false);
  public readonly error = signal<string | null>(null);
  public readonly successMessage = signal<string | null>(null);
  public readonly passwordInputType = computed(() => (this.showPassword() ? 'text' : 'password'));
  public readonly passwordIcon = computed(() =>
    this.showPassword() ? 'visibility_off' : 'visibility'
  );

  private readonly _documentTypeValue = toSignal(this.form.controls.documentType.valueChanges, {
    initialValue: this.form.controls.documentType.value,
  });

  public readonly isCedulaInput = computed(() => this._documentTypeValue() === 'CI');

  constructor() {
    effect(() => {
      const validators = this.isCedulaInput()
        ? CEDULA_DOCUMENT_NUMBER_VALIDATORS
        : NON_CEDULA_DOCUMENT_NUMBER_VALIDATORS;

      this.form.controls.documentNumber.setValidators(validators);
      this.form.controls.documentNumber.updateValueAndValidity({ emitEvent: false });
    });
  }

  public togglePasswordVisibility(): void {
    this.showPassword.update(value => !value);
  }

  public submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    const { documentType, password } = this.form.getRawValue();
    let { documentNumber } = this.form.getRawValue();
    if (documentType === 'CI') {
      documentNumber = documentNumber.replaceAll('.', '');
    }

    this.auth
      .login({ documentType, documentNumber, password })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.successMessage.set('Sesión iniciada correctamente.');
          this.form.controls.password.reset('');
          void this.router.navigateByUrl('/home');
        },
        error: error => {
          this.error.set(this.getErrorMessage(error));
        },
      });
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof AuthRequestError) {
      return `No se pudo iniciar sesión. Error ${error.status || 'de red'}.`;
    }

    return 'No se pudo iniciar sesión.';
  }
}

