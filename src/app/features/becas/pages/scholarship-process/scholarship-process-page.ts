import { inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';

import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { ScholarshipVariant } from '../../models/scholarship-personal-forms';

export abstract class ScholarshipProcessPage {
  protected abstract readonly variant: ScholarshipVariant;

  protected readonly onboardingCompleted = signal(false);
  protected readonly success = signal(false);

  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);

  protected readonly process = inject(ScholarshipProcessFacade);

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
