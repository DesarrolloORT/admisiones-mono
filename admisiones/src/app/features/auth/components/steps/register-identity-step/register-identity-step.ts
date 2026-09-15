import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  type OrtErrorItem,
  OrtFileUploaderChange,
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtProgressBar,
} from '@desarrolloort/components';
import {
  ACCEPTED_IMAGE_MIME_TYPES,
  MAX_IMAGE_SIZE_BEFORE_COMPRESSION_BYTES,
} from 'src/app/shared/files/image-upload';
import {
  buildFormErrorSummary,
  focusFieldById,
  FormErrorField,
  getFirstInvalidFieldId,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';

import { IdentityForm } from '../../../forms/auth-forms';
import { DocumentFields } from '../../shared/document-fields/document-fields';

@Component({
  selector: 'app-register-identity-step',
  imports: [
    DocumentFields,
    OrtButtonModule,
    OrtFileUploaderModule,
    OrtFormFieldModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
    OrtProgressBar,
  ],
  templateUrl: './register-identity-step.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterIdentityStep {
  private readonly document = inject(DOCUMENT);
  private readonly errorSummaryAnchor = viewChild<ElementRef<HTMLElement>>('errorSummaryAnchor');

  public readonly form = input.required<FormGroup<IdentityForm>>();
  public readonly documentNumberLabel = input('Nro. de documento');
  public readonly acceptedDocumentTypes = input<string[]>([...ACCEPTED_IMAGE_MIME_TYPES]);
  public readonly maxDocumentFileSize = MAX_IMAGE_SIZE_BEFORE_COMPRESSION_BYTES;
  public readonly isSubmitting = input(false);
  public readonly isRecognizingDocument = input(false);
  public readonly showLoginAction = input(false);

  public readonly documentSelected = output<File | null>();
  public readonly continueStep = output<void>();
  public readonly loginAction = output<void>();

  public onDocumentFilesChanged(change: OrtFileUploaderChange): void {
    this.documentSelected.emit(change.value.find(file => file.isValid)?.file ?? null);
  }

  public readonly submitted = signal(false);
  public readonly errorSummary = signal<OrtErrorItem[]>([]);

  public refreshErrorSummary(): void {
    if (!this.submitted()) {
      return;
    }

    this.errorSummary.set(
      buildFormErrorSummary(
        this.form(),
        this.currentErrorFields(),
        ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
      )
    );
  }

  public onSubmit(): void {
    this.submitted.set(true);
    this.form().markAllAsTouched();
    this.refreshErrorSummary();

    if (this.form().invalid) {
      this.focusSummaryThenFirstInvalidField();
      return;
    }

    this.continueStep.emit();
  }

  private currentErrorFields(): FormErrorField[] {
    return [
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
    ];
  }

  private focusSummaryThenFirstInvalidField(): void {
    setTimeout(() => {
      const errorSummaryAnchor = this.errorSummaryAnchor()?.nativeElement;

      errorSummaryAnchor?.focus();
      setTimeout(() => {
        if (errorSummaryAnchor && this.document.activeElement !== errorSummaryAnchor) {
          return;
        }

        focusFieldById(
          this.document,
          getFirstInvalidFieldId(this.form(), this.currentErrorFields())
        );
      }, 700);
    });
  }
}
