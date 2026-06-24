import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
  OrtDatePickerModule,
  OrtFormFieldModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { LocationSelect } from '../../../catalogs/components/location-select/location-select';
import { PersonalForm } from '../../forms/auth-forms';
import { RegisterPersonalContactFields } from '../register-personal-contact-fields/register-personal-contact-fields';

@Component({
  selector: 'app-register-personal-complete-fields',
  imports: [
    LocationSelect,
    OrtDatePickerModule,
    OrtFormFieldModule,
    OrtInputModule,
    OrtSelectModule,
    RegisterPersonalContactFields,
    ReactiveFormsModule,
  ],
  templateUrl: './register-personal-complete-fields.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPersonalCompleteFields {
  public readonly form = input.required<FormGroup<PersonalForm>>();
  public readonly submitted = input(false);

  protected readonly maxBirthDate = createDateOnly(
    new Date(new Date().getFullYear() - 18, new Date().getMonth(), new Date().getDate())
  );
}

function createDateOnly(value: Date): Date {
  return new Date(value.getFullYear(), value.getMonth(), value.getDate());
}
