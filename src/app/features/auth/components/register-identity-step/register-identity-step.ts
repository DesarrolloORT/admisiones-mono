import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';
import { Observable } from 'rxjs';
import {
  buildFormErrorSummary,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';

import { DocumentType } from '../../../catalogs/models/catalog.interface';
import { IdentityForm } from '../../forms/auth-forms';

@Component({
  selector: 'app-register-identity-step',
  imports: [
    AsyncPipe,
    OrtButtonModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtInputModule,
    OrtSelectModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './register-identity-step.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterIdentityStep {
  public readonly form = input.required<FormGroup<IdentityForm>>();
  public readonly documentTypes = input.required<Observable<DocumentType[]>>();
  public readonly isCedulaInput = input(false);
  public readonly documentNumberLabel = input('Nro. de documento');
  public readonly selectedFileName = input<string | null>(null);
  public readonly isSubmitting = input(false);
  public readonly isRecognizingDocument = input(false);
  public readonly error = input<string | null>(null);
  public readonly recognitionError = input<string | null>(null);
  public readonly recognitionSuccessMessage = input<string | null>(null);
  public readonly showLoginAction = input(false);

  public readonly documentSelected = output<Event>();
  public readonly continueStep = output<void>();
  public readonly loginAction = output<void>();

  public readonly submitted = signal(false);
  public readonly errorSummary = computed(() => {
    if (!this.submitted()) {
      return [];
    }

    return buildFormErrorSummary(
      this.form(),
      [
        {
          controlName: 'documentType',
          fieldId: 'register-document-type',
          label: 'Tipo de documento',
        },
        {
          controlName: 'documentNumber',
          fieldId: 'register-document-number',
          label: this.documentNumberLabel(),
        },
      ],
      ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
    );
  });

  public onSubmit(): void {
    this.submitted.set(true);
    this.continueStep.emit();
  }
}
