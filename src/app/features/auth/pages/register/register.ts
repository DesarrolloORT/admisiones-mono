import { DOCUMENT } from '@angular/common';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  Injector,
} from '@angular/core';

import { AuthForm } from '../../components/shared/auth-form/auth-form';
import { RegisterIdentityStep } from '../../components/steps/register-identity-step/register-identity-step';
import { RegisterPersonalStep } from '../../components/steps/register-personal-step/register-personal-step';
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
  private readonly document = inject(DOCUMENT);
  private readonly injector = inject(Injector);
  protected readonly facade = inject(RegisterFlowFacade);

  constructor() {
    effect(() => {
      this.facade.step();
      afterNextRender(() => this.focusCurrentStep(), { injector: this.injector });
    });
  }

  private focusCurrentStep(): void {
    const main = this.document.getElementById('main-content');

    main?.focus();
    this.document.defaultView?.scrollTo({ behavior: 'instant', left: 0, top: 0 });
  }
}
