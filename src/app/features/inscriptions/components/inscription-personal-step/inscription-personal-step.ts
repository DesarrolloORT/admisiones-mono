import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { OrtAccordionModule, OrtButtonModule, OrtIconModule } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { InscripcionSurveyFacade } from '../../facades/inscription-survey';
import { InscripcionAcademicDecisionSection } from '../inscription-academic-decision-section/inscription-academic-decision-section';
import { InscripcionEducationSection } from '../inscription-education-section/inscription-education-section';
import { InscripcionIdentitySection } from '../inscription-identity-section/inscription-identity-section';
import { InscripcionOrtExperienceSection } from '../inscription-ort-experience-section/inscription-ort-experience-section';
import { InscripcionRegulationSection } from '../inscription-regulation-section/inscription-regulation-section';
import { InscripcionWorkSection } from '../inscription-work-section/inscription-work-section';

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
  ],
  templateUrl: './inscription-personal-step.html',
  styleUrls: ['../../pages/inscription/inscription.scss', './inscription-personal-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionPersonalStep {
  protected readonly facade = inject(InscripcionSurveyFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly radioGroupOrientation = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'vertical' : 'horizontal';
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
