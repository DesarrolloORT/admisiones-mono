import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import {
  OrtAccordionModule,
  OrtBadgeModule,
  OrtButtonModule,
  OrtIconModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { EnrollmentSurveyFacade } from '../../../facades/enrollment-survey';
import { EnrollmentAcademicDecisionSection } from './sections/enrollment-academic-decision-section/enrollment-academic-decision-section';
import { EnrollmentEducationSection } from './sections/enrollment-education-section/enrollment-education-section';
import { EnrollmentIdentitySection } from './sections/enrollment-identity-section/enrollment-identity-section';
import { EnrollmentOrtExperienceSection } from './sections/enrollment-ort-experience-section/enrollment-ort-experience-section';
import { EnrollmentRegulationSection } from './sections/enrollment-regulation-section/enrollment-regulation-section';
import { EnrollmentWorkSection } from './sections/enrollment-work-section/enrollment-work-section';

const DESKTOP_MIN_WIDTH = 840;

@Component({
  selector: 'app-enrollment-personal-step',
  imports: [
    ErrorAlert,
    EnrollmentAcademicDecisionSection,
    EnrollmentEducationSection,
    EnrollmentIdentitySection,
    EnrollmentOrtExperienceSection,
    EnrollmentRegulationSection,
    EnrollmentWorkSection,
    OrtAccordionModule,
    OrtButtonModule,
    OrtIconModule,
    OrtBadgeModule,
  ],
  templateUrl: './enrollment-personal-step.html',
  styleUrls: ['../../layout.scss', './enrollment-personal-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentPersonalStep {
  protected readonly facade = inject(EnrollmentSurveyFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly radioGroupOrientation = computed(() => {
    return this.breakpointService.breakpoint().screenWidth >= DESKTOP_MIN_WIDTH
      ? 'horizontal'
      : 'vertical';
  });

  protected onFormEnter(event: Event): void {
    if (event.target instanceof HTMLInputElement && event.target.type === 'radio') {
      event.preventDefault();
    }
  }

  protected onSubmit(event: SubmitEvent): void {
    event.preventDefault();
    this.facade.continue();
  }
}
