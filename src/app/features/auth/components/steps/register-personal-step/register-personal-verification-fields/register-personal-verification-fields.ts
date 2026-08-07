import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { OrtAlertModule, OrtFormFieldModule, OrtInputModule } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { PersonalForm } from '../../../../forms/auth-forms';

@Component({
  selector: 'app-register-personal-verification-fields',
  imports: [OrtAlertModule, OrtFormFieldModule, OrtInputModule, ReactiveFormsModule],
  templateUrl: './register-personal-verification-fields.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPersonalVerificationFields {
  public readonly form = input.required<FormGroup<PersonalForm>>();

  private readonly breakpointService = inject(BreakpointService);

  public readonly alertLayout = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isMedium || breakpoint.isLarge ? 'stacked' : 'inline';
  });
}
