import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { PersonalForm } from '../../forms/auth-forms';
import { LocationSelect } from '../location-select/location-select';

@Component({
  selector: 'app-register-personal-step',
  imports: [
    LocationSelect,
    OrtButtonModule,
    OrtFormFieldModule,
    OrtInputModule,
    OrtSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './register-personal-step.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPersonalStep {
  public readonly form = input.required<FormGroup<PersonalForm>>();
  public readonly isVerificationOnly = input(false);
  public readonly isSubmitting = input(false);
  public readonly error = input<string | null>(null);
  public readonly successMessage = input<string | null>(null);

  public readonly back = output<void>();
  public readonly submitStep = output<void>();
}
