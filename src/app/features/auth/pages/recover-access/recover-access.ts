import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OrtButtonModule, OrtFormFieldModule, OrtInputModule } from '@desarrolloort/components';
import { isNormalizedApiError } from '@desarrolloort/ngx-utils';
import { finalize } from 'rxjs/operators';

import {
  focusFieldById,
  FormErrorField,
  getFirstInvalidFieldId,
} from '../../../../shared/forms/form-error-summary';
import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { AuthForm } from '../../components/auth-form/auth-form';
import { DocumentFields } from '../../components/document-fields/document-fields';
import { createRecoverAccessForm } from '../../forms/auth-forms';
import { formatDocumentForBackend } from '../../models/document-number';
import { PasswordActivationService } from '../../services/password-activation';

@Component({
  selector: 'app-recover-access',
  imports: [
    AuthForm,
    DocumentFields,
    OrtFormFieldModule,
    OrtInputModule,
    OrtButtonModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './recover-access.html',
  styleUrl: './recover-access.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecoverAccess {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly passwordService = inject(PasswordActivationService);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarHandler);

  protected readonly form = createRecoverAccessForm();
  protected readonly isSubmitting = signal(false);
  protected readonly submitted = signal(false);
  private readonly errorFields: FormErrorField[] = [
    {
      controlName: 'documentType',
      fieldId: 'recover-document-type',
      label: 'Tipo de documento',
    },
    {
      controlName: 'documentNumber',
      fieldId: 'recover-document-number',
      label: 'Nro. de documento',
      messages: {
        pattern: 'Ingresá solo caracteres alfanuméricos o guiones.',
      },
    },
    {
      controlName: 'primerApellido',
      fieldId: 'recover-primer-apellido',
      label: 'Primer apellido',
    },
  ];

  protected submit(): void {
    this.submitted.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackbar.error('Revisá los campos marcados.');
      focusFieldById(this.document, getFirstInvalidFieldId(this.form, this.errorFields));
      return;
    }

    this.isSubmitting.set(true);

    const { documentType, documentNumber, primerApellido } = this.form.getRawValue();

    this.passwordService
      .recoverPassword({
        tipoDocumento: documentType,
        documento: formatDocumentForBackend(documentType, documentNumber),
        primerApellido: primerApellido.trim(),
      })
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => this.completeRequest(),
        error: error => {
          if (isNormalizedApiError(error) && error.status >= 400 && error.status < 500) {
            this.completeRequest();
            return;
          }

          this.snackbar.error('No se pudo procesar la solicitud. Intentá nuevamente.');
        },
      });
  }

  private completeRequest(): void {
    void this.router.navigateByUrl('/confirmacion-correo/recuperar-acceso');
  }
}
