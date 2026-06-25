import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { ScholarshipOnboarding } from '../../components/scholarship-onboarding/scholarship-onboarding';
import { ScholarshipPersonalStep } from '../../components/scholarship-personal-step/scholarship-personal-step';
import { ScholarshipPostulationStep } from '../../components/scholarship-postulation-step/scholarship-postulation-step';
import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { ScholarshipProcessStore } from '../../store/scholarship-process';

@Component({
  selector: 'app-fbr',
  imports: [
    ProcessLayout,
    ScholarshipOnboarding,
    ScholarshipPostulationStep,
    ScholarshipPersonalStep,
  ],
  providers: [ScholarshipProcessStore, ScholarshipProcessFacade],
  templateUrl: './fbr.html',
  styleUrl: './fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Fbr {
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);

  protected readonly process = inject(ScholarshipProcessFacade);

  protected goHome(): void {
    void this.router.navigate(['/inicio']);
  }

  protected logout(): void {
    this.authSession.logout();
  }
}
