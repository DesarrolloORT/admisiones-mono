import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { isNormalizedApiError } from '@desarrolloort/ngx-utils';
import { finalize } from 'rxjs/operators';

import {
  buildFormErrorSummary,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from '../../../shared/forms/form-error-summary';
import { Catalogs } from '../../catalogs/services/catalogs';
import { createLoginForm, syncDocumentNumberValidators } from '../forms/auth-forms';
import { cleanDocumentNumber, isCedulaDocumentType } from '../models/document-number';
import { AuthSessionService } from '../services/auth-session';

@Injectable({
  providedIn: 'root',
})
export class LoginFacade {
  private readonly authSession = inject(AuthSessionService);
  private readonly catalogs = inject(Catalogs);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  public readonly documentTypes$ = this.catalogs.getDocumentTypes();
  public readonly form = createLoginForm();
  public readonly isSubmitting = signal(false);
  public readonly showPassword = signal(false);
  public readonly error = signal<string | null>(null);
  public readonly successMessage = signal<string | null>(null);
  public readonly submitted = signal(false);
  public readonly passwordInputType = computed(() => (this.showPassword() ? 'text' : 'password'));
  public readonly passwordIcon = computed(() =>
    this.showPassword() ? 'visibility_off' : 'visibility'
  );
  public readonly passwordToggleLabel = computed(() =>
    this.showPassword() ? 'Ocultar contraseña' : 'Mostrar contraseña'
  );
  public readonly errorSummary = computed(() =>
    this.submitted()
      ? buildFormErrorSummary(
          this.form,
          [
            {
              controlName: 'documentType',
              fieldId: 'login-document-type',
              label: 'Tipo de documento',
            },
            {
              controlName: 'documentNumber',
              fieldId: 'login-document-number',
              label: 'Nro. de documento',
              messages: {
                pattern: 'Ingresá solo caracteres alfanuméricos.',
              },
            },
            {
              controlName: 'password',
              fieldId: 'login-password',
              label: 'Contraseña',
            },
          ],
          ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
        )
      : []
  );

  private readonly _documentTypeValue = toSignal(this.form.controls.documentType.valueChanges, {
    initialValue: this.form.controls.documentType.value,
  });

  public readonly isCedulaInput = computed(() => isCedulaDocumentType(this._documentTypeValue()));

  public readonly prefilled = signal(false);

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

    if (tipoDoc && doc) {
      this.prefilled.set(true);
    }
  }

  public togglePasswordVisibility(): void {
    this.showPassword.update(value => !value);
  }

  public submit(): void {
    this.submitted.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.successMessage.set(null);
    this.isSubmitting.set(true);

    const { documentType, documentNumber, password } = this.form.getRawValue();

    this.authSession
      .login({
        documentType,
        documentNumber: cleanDocumentNumber(documentType, documentNumber),
        password,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.successMessage.set('Sesión iniciada correctamente.');
          this.form.controls.password.reset('');
          void this.router.navigateByUrl('/inicio');
        },
        error: error => {
          this.setError(this.getApiErrorMessage(error, 'No se pudo iniciar sesión.'));
        },
      });
  }

  private setError(message: string): void {
    this.error.set(message);
  }

  private getApiErrorMessage(error: unknown, fallback: string): string {
    return isNormalizedApiError(error) ? error.message : fallback;
  }
}
