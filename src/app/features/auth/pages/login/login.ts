import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
} from '@desarrolloort/components';

import {
  focusFieldById,
  FormErrorField,
  getFirstInvalidFieldId,
} from '../../../../shared/forms/form-error-summary';
import { createPasswordVisibility } from '../../../../shared/forms/password-visibility';
import { ErrorAlert } from '../../../../shared/ui/error-alert/error-alert';
import { AuthForm } from '../../components/shared/auth-form/auth-form';
import { DocumentFields } from '../../components/shared/document-fields/document-fields';
import { createLoginForm } from '../../forms/auth-forms';
import { getApiErrorMessage } from '../../models/api-error-message';
import { cleanDocumentNumber } from '../../models/document-number';
import { AuthSessionService } from '../../services/auth-session';

@Component({
  selector: 'app-login',
  imports: [
    AuthForm,
    DocumentFields,
    OrtFormFieldModule,
    OrtInputModule,
    OrtButtonModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
    ErrorAlert,
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);
  protected readonly form = createLoginForm();
  protected readonly isSubmitting = signal(false);
  protected readonly passwordVisibility = createPasswordVisibility();
  protected readonly submitted = signal(false);
  protected readonly formError = signal<string | null>(null);
  private readonly errorFields: FormErrorField[] = [
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
        pattern: 'Ingresá solo caracteres alfanuméricos o guiones.',
      },
    },
    {
      controlName: 'password',
      fieldId: 'login-password',
      label: 'Contraseña',
    },
  ];
  protected submit(): void {
    this.submitted.set(true);
    this.formError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Revisá los campos marcados.');
      focusFieldById(this.document, getFirstInvalidFieldId(this.form, this.errorFields));
      return;
    }

    this.isSubmitting.set(true);

    const { documentType, documentNumber, password } = this.form.getRawValue();
    const cleanedDocumentNumber = cleanDocumentNumber(documentType, documentNumber);

    this.authSession
      .login({
        documentType,
        documentNumber: cleanedDocumentNumber,
        password,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: outcome => {
          if (outcome.kind === 'twoFactorRequired') {
            this.form.controls.password.reset('');
            this.router
              .navigateByUrl('/confirmacion-correo/verificar-codigo')
              .finally(() => this.isSubmitting.set(false));
            return;
          }

          this.form.controls.password.reset('');
          this.router.navigateByUrl('/inicio').finally(() => this.isSubmitting.set(false));
        },
        error: error => {
          const message = getApiErrorMessage(error, 'No se pudo iniciar sesión.');
          this.isSubmitting.set(false);
          this.formError.set(message);
        },
      });
  }
}
