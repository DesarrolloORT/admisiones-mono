import { ChangeDetectionStrategy, Component, computed, input, viewChild } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtInputModule, OrtPhoneInput } from '@desarrolloort/components';
import { phoneMaxDigits } from 'src/app/shared/forms/phone';

import { PersonalForm } from '../../../../../forms/auth-forms';

@Component({
  selector: 'app-register-personal-contact-fields',
  imports: [OrtFormFieldModule, OrtInputModule, ReactiveFormsModule],
  templateUrl: './register-personal-contact-fields.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPersonalContactFields {
  public readonly form = input.required<FormGroup<PersonalForm>>();

  // El control es `updateOn: 'blur'`, asi que su valor no refleja el pais recien elegido:
  // el largo se toma del propio input.
  private readonly phoneField = viewChild(OrtPhoneInput);
  protected readonly phoneMaxLength = computed(() =>
    phoneMaxDigits(this.phoneField()?.selectedCountry())
  );
}
