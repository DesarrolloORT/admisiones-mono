import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AuthForm } from '../../components/auth-form/auth-form';
import { RegisterIdentityStep } from '../../components/register-identity-step/register-identity-step';
import { RegisterPersonalStep } from '../../components/register-personal-step/register-personal-step';
import { RegisterFlowFacade } from '../../facades/register-flow.facade';

@Component({
  selector: 'app-register',
  imports: [AuthForm, RegisterIdentityStep, RegisterPersonalStep],
  providers: [RegisterFlowFacade],
  templateUrl: './register.html',
  styleUrl: './register.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  protected readonly facade = inject(RegisterFlowFacade);
}
