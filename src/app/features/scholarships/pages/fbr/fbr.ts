import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { AcademicProposalSelection } from '../../../catalogs/services/academic-proposal-selection';
import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { ScholarshipProposalFacade } from '../../facades/scholarship-proposal';
import { ScholarshipAcademicStep } from './steps/scholarship-academic-step/scholarship-academic-step';
import { ScholarshipPersonalStep } from './steps/scholarship-personal-step/scholarship-personal-step';

@Component({
  selector: 'app-fbr',
  imports: [ProcessLayout, ScholarshipAcademicStep, ScholarshipPersonalStep],
  providers: [AcademicProposalSelection, ScholarshipProposalFacade, ScholarshipProcessFacade],
  templateUrl: './fbr.html',
  styleUrl: './fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Fbr {
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);

  protected readonly process = inject(ScholarshipProcessFacade);

  protected goHome(): void {
    this.router.navigate(['/inicio']);
  }

  protected logout(): void {
    this.authSession.logout();
  }
}
