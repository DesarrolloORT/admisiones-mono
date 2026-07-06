import { ChangeDetectionStrategy, Component } from '@angular/core';
import { HomeHeader } from 'src/app/shared/ui/home-header/home-header';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { ScholarshipAcademicStep } from '../../components/scholarship-academic-step/scholarship-academic-step';
import { ScholarshipConfirmationStep } from '../../components/scholarship-confirmation-step/scholarship-confirmation-step';
import { ScholarshipOnboarding } from '../../components/scholarship-onboarding/scholarship-onboarding';
import { ScholarshipPersonalStep } from '../../components/scholarship-personal-step/scholarship-personal-step';
import { ScholarshipSuccess } from '../../components/scholarship-success/scholarship-success';
import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { ScholarshipProposalFacade } from '../../facades/scholarship-proposal';
import { ScholarshipVariant } from '../../models/scholarship-personal-forms';
import { ScholarshipFormsStore } from '../../store/scholarship-forms';
import { ScholarshipProcessStore } from '../../store/scholarship-process';
import { ScholarshipProcessPage } from '../scholarship-process/scholarship-process-page';

@Component({
  selector: 'app-fexa',
  imports: [
    ProcessLayout,
    ScholarshipOnboarding,
    ScholarshipAcademicStep,
    ScholarshipPersonalStep,
    HomeHeader,
    ScholarshipConfirmationStep,
    ScholarshipSuccess,
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
export class Fexa extends ScholarshipProcessPage {
  protected readonly variant: ScholarshipVariant = 'fexaCon';
}
