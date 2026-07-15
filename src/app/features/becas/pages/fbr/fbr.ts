import { ChangeDetectionStrategy, Component } from '@angular/core';
import { HomeHeader } from 'src/app/shared/ui/home-header/home-header';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { ScholarshipAcademicStep } from '../../components/steps/scholarship-academic-step/scholarship-academic-step';
import { ScholarshipConfirmationStep } from '../../components/steps/scholarship-confirmation-step/scholarship-confirmation-step';
import { ScholarshipOnboardingStep } from '../../components/steps/scholarship-onboarding-step/scholarship-onboarding-step';
import { ScholarshipPersonalStep } from '../../components/steps/scholarship-personal-step/scholarship-personal-step';
import { ScholarshipSuccessStep } from '../../components/steps/scholarship-success-step/scholarship-success-step';
import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { ScholarshipProposalFacade } from '../../facades/scholarship-proposal';
import { ScholarshipVariant } from '../../models/scholarship-personal-forms';
import { ScholarshipFormsStore } from '../../store/scholarship-forms';
import { ScholarshipProcessStore } from '../../store/scholarship-process';
import { ScholarshipProcessPage } from '../scholarship-process/scholarship-process-page';

@Component({
  selector: 'app-fbr',
  imports: [
    ProcessLayout,
    ScholarshipOnboardingStep,
    ScholarshipAcademicStep,
    ScholarshipPersonalStep,
    HomeHeader,
    ScholarshipConfirmationStep,
    ScholarshipSuccessStep,
  ],
  providers: [
    ScholarshipFormsStore,
    ScholarshipProcessStore,
    ScholarshipProcessFacade,
    ScholarshipProposalFacade,
  ],
  templateUrl: '../scholarship-process/scholarship-process.html',
  styleUrl: '../scholarship-process/scholarship-process.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Fbr extends ScholarshipProcessPage {
  protected readonly variant: ScholarshipVariant = 'fbr';
}
