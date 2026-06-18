import { AsyncPipe, DOCUMENT } from '@angular/common';
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
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';
import { Observable } from 'rxjs';
import {
  buildFormErrorSummary,
  focusFieldById,
  FormErrorField,
  getFirstInvalidFieldId,
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
  private readonly document = inject(DOCUMENT);
  private readonly errorSummaryAnchor = viewChild<ElementRef<HTMLElement>>('errorSummaryAnchor');

  public readonly form = input.required<FormGroup<IdentityForm>>();
  public readonly documentTypes = input.required<Observable<DocumentType[]>>();
  public readonly isCedulaInput = input(false);
  public readonly documentNumberLabel = input('Nro. de documento');
  public readonly selectedFileName = input<string | null>(null);
  public readonly isSubmitting = input(false);
  public readonly isRecognizingDocument = input(false);
  public readonly showLoginAction = input(false);

  public readonly documentSelected = output<Event>();
  public readonly continueStep = output<void>();
  public readonly loginAction = output<void>();

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
