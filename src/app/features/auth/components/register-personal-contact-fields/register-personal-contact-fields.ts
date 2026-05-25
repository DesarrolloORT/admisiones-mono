import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtInputModule } from '@desarrolloort/components';

import { PersonalForm } from '../../forms/auth-forms';

@Component({
  selector: 'app-register-personal-contact-fields',
  imports: [OrtFormFieldModule, OrtInputModule, ReactiveFormsModule],
  templateUrl: './register-personal-contact-fields.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPersonalContactFields {
  public readonly form = input.required<FormGroup<PersonalForm>>();
}
