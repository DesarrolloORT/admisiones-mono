import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';
import { isNormalizedApiError } from '@desarrolloort/ngx-utils';
import { finalize } from 'rxjs/operators';

import {
  focusFieldById,
  FormErrorField,
  getFirstInvalidFieldId,
} from '../../../../shared/forms/form-error-summary';
import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { AuthForm } from '../../components/auth-form/auth-form';
import { createRecoverAccessForm, syncDocumentNumberValidators } from '../../forms/auth-forms';
import { formatDocumentForBackend, isCedulaDocumentType } from '../../models/document-number';
import { PasswordActivationService } from '../../services/password-activation';

@Component({
  selector: 'app-recover-access',
  imports: [
    AuthForm,
    OrtFormFieldModule,
    OrtInputModule,
    OrtSelectModule,
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
  private readonly passwordService = inject(PasswordActivationService);
  private readonly route = inject(ActivatedRoute);
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
        pattern: 'Ingresá solo caracteres alfanuméricos.',
      },
    },
    {
      controlName: 'primerApellido',
      fieldId: 'recover-primer-apellido',
      label: 'Primer apellido',
    },
  ];

  private readonly documentTypeValue = toSignal(this.form.controls.documentType.valueChanges, {
    initialValue: this.form.controls.documentType.value,
  });

  protected readonly isCedulaInput = computed(() => isCedulaDocumentType(this.documentTypeValue()));

  constructor() {
    effect(() => {
      syncDocumentNumberValidators(this.form.controls.documentNumber, this.documentTypeValue());
    });

    this.prefillFromQueryParams();
  }

  protected submit(): void {
    this.submitted.set(true);
    syncDocumentNumberValidators(this.form.controls.documentNumber, this.documentTypeValue());

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
        primerApellido,
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl('/confirmacion-correo/recuperar-acceso');
        },
        error: error => {
          const message = this.getApiErrorMessage(
            error,
            'No se pudo procesar la solicitud. Intentá nuevamente.'
          );
          this.snackbar.error(message);
        },
      });
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

  private getApiErrorMessage(error: unknown, fallback: string): string {
    return isNormalizedApiError(error) ? error.message : fallback;
  }
}
