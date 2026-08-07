import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { AbstractControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule, type OrtErrorItem, OrtFormFieldModule } from '@desarrolloort/components';
import { filter, firstValueFrom } from 'rxjs';
import {
  buildFormErrorSummary,
  focusFieldById,
  FormErrorField,
  getFirstInvalidFieldId,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';

import { PersonalForm } from '../../../forms/auth-forms';
import { RegisterPersonalMode } from '../../../models/register-flow';
import { RegisterPersonalCompleteFields } from './register-personal-complete-fields/register-personal-complete-fields';
import { RegisterPersonalVerificationFields } from './register-personal-verification-fields/register-personal-verification-fields';

@Component({
  selector: 'app-register-personal-step',
  imports: [
    OrtButtonModule,
    OrtFormFieldModule,
    RegisterPersonalCompleteFields,
    RegisterPersonalVerificationFields,
    ReactiveFormsModule,
  ],
  templateUrl: './register-personal-step.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPersonalStep {
  private readonly document = inject(DOCUMENT);
  private readonly errorSummaryAnchor = viewChild<ElementRef<HTMLElement>>('errorSummaryAnchor');

  public readonly form = input.required<FormGroup<PersonalForm>>();
  public readonly personalMode = input<RegisterPersonalMode>('complete');
  public readonly isSubmitting = input(false);
  public readonly isCompleted = input(false);

  public readonly back = output<void>();
  public readonly submitStep = output<void>();

  public readonly submitted = signal(false);
  public readonly errorSummary = signal<OrtErrorItem[]>([]);
  protected readonly submitLabel = computed(() =>
    this.personalMode() === 'verification' ? 'Confirmar' : 'Crear cuenta'
  );
  protected readonly submittingLabel = computed(() =>
    this.personalMode() === 'verification' ? 'Confirmando...' : 'Creando...'
  );
  private readonly verificationFields: FormErrorField[] = [
    { controlName: 'primerApellido', fieldId: 'first-last-name', label: 'Primer apellido' },
    { controlName: 'mail', fieldId: 'email', label: 'E-mail' },
  ];
  private readonly completeFields: FormErrorField[] = [
    { controlName: 'primerNombre', fieldId: 'first-name', label: 'Primer nombre' },
    { controlName: 'primerApellido', fieldId: 'first-last-name', label: 'Primer apellido' },
    { controlName: 'fechaNacimiento', fieldId: 'birth-date', label: 'Fecha de nacimiento' },
    { controlName: 'sexo', fieldId: 'sex', label: 'Sexo' },
    {
      controlName: 'location',
      fieldId: (control: AbstractControl) => {
        if (control.hasError('locationStateRequired')) {
          return 'location-state';
        }

        if (control.hasError('locationCityRequired')) {
          return 'location-city';
        }

        return 'location-country';
      },
      label: 'Ubicación',
    },
    { controlName: 'direccion', fieldId: 'address', label: 'Dirección' },
    { controlName: 'telefono1', fieldId: 'phone', label: 'Celular' },
    { controlName: 'mail', fieldId: 'email', label: 'E-mail' },
    { controlName: 'verificacionMail', fieldId: 'confirm-email', label: 'Confirmar e-mail' },
  ];

  public hasSubmittedInvalidFields(): boolean {
    if (!this.submitted()) {
      return false;
    }

    if (this.personalMode() === 'verification') {
      const { primerApellido, mail } = this.form().controls;

      return primerApellido.invalid || mail.invalid;
    }

    return this.form().invalid;
  }

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

  public async onSubmit(): Promise<void> {
    this.submitted.set(true);
    this.form().markAllAsTouched();

    if (this.form().pending) {
      await firstValueFrom(this.form().statusChanges.pipe(filter(status => status !== 'PENDING')));
    }

    this.refreshErrorSummary();

    if (this.hasSubmittedInvalidFields()) {
      this.focusSummaryThenFirstInvalidField();
      return;
    }

    this.submitStep.emit();
  }

  private currentErrorFields(): FormErrorField[] {
    return this.personalMode() === 'verification' ? this.verificationFields : this.completeFields;
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
