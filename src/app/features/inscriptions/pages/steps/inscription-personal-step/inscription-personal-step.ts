import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import {
  OrtAccordionModule,
  OrtBadgeModule,
  OrtButtonModule,
  OrtIconModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { InscripcionSurveyFacade } from '../../../facades/inscription-survey';
import { InscripcionAcademicDecisionSection } from './sections/inscription-academic-decision-section/inscription-academic-decision-section';
import { InscripcionEducationSection } from './sections/inscription-education-section/inscription-education-section';
import { InscripcionIdentitySection } from './sections/inscription-identity-section/inscription-identity-section';
import { InscripcionOrtExperienceSection } from './sections/inscription-ort-experience-section/inscription-ort-experience-section';
import { InscripcionRegulationSection } from './sections/inscription-regulation-section/inscription-regulation-section';
import { InscripcionWorkSection } from './sections/inscription-work-section/inscription-work-section';

const DESKTOP_MIN_WIDTH = 840;

@Component({
  selector: 'app-inscription-personal-step',
  imports: [
    ErrorAlert,
    InscripcionAcademicDecisionSection,
    InscripcionEducationSection,
    InscripcionIdentitySection,
    InscripcionOrtExperienceSection,
    InscripcionRegulationSection,
    InscripcionWorkSection,
    OrtAccordionModule,
    OrtButtonModule,
    OrtIconModule,
    OrtBadgeModule,
  ],
  templateUrl: './inscription-personal-step.html',
  styleUrls: ['../../layout.scss', './inscription-personal-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionPersonalStep {
  protected readonly facade = inject(InscripcionSurveyFacade);
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
