import { DOCUMENT } from '@angular/common';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  Injector,
} from '@angular/core';

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
  private readonly document = inject(DOCUMENT);
  private readonly injector = inject(Injector);
  protected readonly facade = inject(RegisterFlowFacade);

  public debugRuns = 0;

  constructor() {
    effect(() => {
      this.facade.step();
      this.debugRuns++;

      afterNextRender(() => this.focusCurrentStep(), { injector: this.injector });
    });
  }

  private focusCurrentStep(): void {
    const main = this.document.getElementById('main-content');

    console.log('DEBUG focusCurrentStep', !!main, main?.outerHTML, typeof main?.focus);
    main?.focus();
    this.document.defaultView?.scrollTo({ behavior: 'instant', left: 0, top: 0 });
  }
}
