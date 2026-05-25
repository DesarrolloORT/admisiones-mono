import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule } from '@desarrolloort/components';

import { PersonalForm } from '../../forms/auth-forms';
import { RegisterPersonalMode } from '../../models/register-flow';
import { RegisterPersonalCompleteFields } from '../register-personal-complete-fields/register-personal-complete-fields';
import { RegisterPersonalVerificationFields } from '../register-personal-verification-fields/register-personal-verification-fields';

@Component({
  selector: 'app-register-personal-step',
  imports: [
    OrtButtonModule,
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

  public onSubmit(): void {
    this.submitted.set(true);
    this.submitStep.emit();
  }
}

