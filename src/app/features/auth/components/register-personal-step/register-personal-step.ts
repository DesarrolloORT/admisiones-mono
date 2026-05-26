import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { AbstractControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule, OrtFormFieldModule } from '@desarrolloort/components';
import { buildFormErrorSummary } from 'src/app/shared/forms/form-error-summary';

import { PersonalForm } from '../../forms/auth-forms';
import { RegisterPersonalMode } from '../../models/register-flow';
import { RegisterPersonalCompleteFields } from '../register-personal-complete-fields/register-personal-complete-fields';
import { RegisterPersonalVerificationFields } from '../register-personal-verification-fields/register-personal-verification-fields';

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
  public readonly form = input.required<FormGroup<PersonalForm>>();
  public readonly personalMode = input<RegisterPersonalMode>('complete');
  public readonly isSubmitting = input(false);
  public readonly error = input<string | null>(null);
  public readonly successMessage = input<string | null>(null);

  public readonly back = output<void>();
  public readonly submitStep = output<void>();

  public readonly submitted = signal(false);
  public readonly hasSubmittedInvalidFields = computed(() => {
    if (!this.submitted()) {
      return false;
    }

    if (this.personalMode() === 'verification') {
      const { primerApellido, mail } = this.form().controls;

      return primerApellido.invalid || mail.invalid;
    }

    return this.form().invalid;
  });
  public readonly errorSummary = computed(() => {
    if (!this.submitted()) {
      return [];
    }

    const verificationFields = [
      { controlName: 'primerApellido', fieldId: 'first-last-name', label: 'Primer apellido' },
      { controlName: 'mail', fieldId: 'email', label: 'E-mail' },
    ];

    const completeFields = [
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

    return buildFormErrorSummary(
      this.form(),
      this.personalMode() === 'verification' ? verificationFields : completeFields
    );
  });

  public onSubmit(): void {
    this.submitted.set(true);
    this.submitStep.emit();
  }
}
