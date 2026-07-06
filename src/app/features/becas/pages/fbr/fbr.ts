import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { HomeHeader } from 'src/app/shared/ui/home-header/home-header';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { ScholarshipAcademicStep } from '../../components/scholarship-academic-step/scholarship-academic-step';
import { ScholarshipConfirmationStep } from '../../components/scholarship-confirmation-step/scholarship-confirmation-step';
import { ScholarshipOnboarding } from '../../components/scholarship-onboarding/scholarship-onboarding';
import { ScholarshipPersonalStep } from '../../components/scholarship-personal-step/scholarship-personal-step';
import { ScholarshipSuccess } from '../../components/scholarship-success/scholarship-success';
import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { ScholarshipFormsStore } from '../../store/scholarship-forms';
import { ScholarshipProcessStore } from '../../store/scholarship-process';

@Component({
  selector: 'app-fbr',
  imports: [
    ProcessLayout,
    ScholarshipOnboarding,
    ScholarshipAcademicStep,
    ScholarshipPersonalStep,
    HomeHeader,
    ScholarshipConfirmationStep,
    ScholarshipSuccess,
  ],
  providers: [ScholarshipFormsStore, ScholarshipProcessStore, ScholarshipProcessFacade],
  templateUrl: './fbr.html',
  styleUrl: './fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Fbr {
  protected readonly onboardingCompleted = signal(false);
  protected readonly success = signal(false);

  protected startApplication(): void {
    this.onboardingCompleted.set(true);
  }
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);

  protected readonly process = inject(ScholarshipProcessFacade);

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
