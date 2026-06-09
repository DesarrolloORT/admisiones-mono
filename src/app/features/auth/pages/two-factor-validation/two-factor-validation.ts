import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AuthForm } from '../../components/auth-form/auth-form';
import { TwoFactorValidation } from '../../components/two-factor-validation/two-factor-validation';
import { TwoFactorValidationFacade } from '../../facades/two-factor-validation.facade';

@Component({
  selector: 'app-two-factor-validation-page',
  imports: [AuthForm, TwoFactorValidation],
  providers: [TwoFactorValidationFacade],
  templateUrl: './two-factor-validation.html',
  styleUrl: './two-factor-validation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TwoFactorValidationPage {
  protected readonly facade: TwoFactorValidationFacade = inject(TwoFactorValidationFacade);
}

