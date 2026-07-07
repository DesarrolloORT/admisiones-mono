import { DOCUMENT } from '@angular/common';
import { effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';

import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { ScholarshipVariant } from '../../models/scholarship-personal-forms';

export abstract class ScholarshipProcessPage {
  protected abstract readonly variant: ScholarshipVariant;

  protected readonly onboardingCompleted = signal(false);
  protected readonly success = signal(false);

  private readonly document = inject(DOCUMENT);
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);

  protected readonly process = inject(ScholarshipProcessFacade);

  constructor() {
    effect(() => {
      this.onboardingCompleted();
      this.process.currentStep();
      this.success();

      setTimeout(() => this.focusCurrentScreen());
    });
  }

  private focusCurrentScreen(): void {
    const main = this.document.getElementById('main-content');
    main?.focus();
    this.document.defaultView?.scrollTo({ behavior: 'instant', left: 0, top: 0 });
    this.document.defaultView?.setTimeout(() => main?.focus(), 50);
  }

  protected startApplication(): void {
    this.onboardingCompleted.set(true);
  }

  protected goHome(): void {
    void this.router.navigate(['/inicio']);
  }

  protected logout(): void {
    this.authSession.logout();
  }

  protected showSuccess(): void {
    this.success.set(true);
  }
}
