import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtInputModule, OrtSelectModule } from '@desarrolloort/components';

import { PersonalForm } from '../../forms/auth-forms';
import { LocationSelect } from '../location-select/location-select';
import { RegisterPersonalContactFields } from '../register-personal-contact-fields/register-personal-contact-fields';

@Component({
  selector: 'app-register-personal-complete-fields',
  imports: [
    LocationSelect,
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
}
