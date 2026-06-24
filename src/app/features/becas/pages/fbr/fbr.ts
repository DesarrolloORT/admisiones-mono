import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ExpandableStepperStep } from '@desarrolloort/components';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { ScholarshipOnboarding } from '../../components/scholarship-onboarding/scholarship-onboarding';
import { ScholarshipPersonalStep } from '../../components/scholarship-personal-step/scholarship-personal-step';
import { ScholarshipPostulationStep } from '../../components/scholarship-postulation-step/scholarship-postulation-step';

const SCHOLARSHIP_STEPS: ExpandableStepperStep[] = [
  {
    id: 'info-postulacion',
    overline: 'Paso 1',
    status: 'current',
    title: 'Información de postulación',
  },
  {
    id: 'info-personal',
    overline: 'Paso 2',
    status: 'pending',
    title: 'Información personal',
  },
  {
    id: 'confirmacion',
    overline: 'Paso 3',
    status: 'pending',
    title: 'Confirmación',
  },
];

@Component({
  selector: 'app-fbr',
  imports: [
    ProcessLayout,
    ScholarshipOnboarding,
    ScholarshipPostulationStep,
    ScholarshipPersonalStep,
  ],
  templateUrl: './fbr.html',
  styleUrl: './fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Fbr {
  private readonly authSession = inject(AuthSessionService);

  private readonly router = inject(Router);

  protected readonly steps = SCHOLARSHIP_STEPS;

  protected goHome(): void {
    void this.router.navigate(['/inicio']);
  }

  protected logout(): void {
    this.authSession.logout();
  }
}
