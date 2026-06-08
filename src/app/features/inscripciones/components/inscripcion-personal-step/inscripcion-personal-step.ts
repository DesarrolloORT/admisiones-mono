import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule } from '@desarrolloort/components';

import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';
import { InscripcionPersonalDecisionSection } from '../inscripcion-personal-decision-section/inscripcion-personal-decision-section';
import { InscripcionPersonalEducationSection } from '../inscripcion-personal-education-section/inscripcion-personal-education-section';
import { InscripcionPersonalWorkSection } from '../inscripcion-personal-work-section/inscripcion-personal-work-section';

@Component({
  selector: 'app-inscripcion-personal-step',
  imports: [
    OrtButtonModule,
    InscripcionPersonalDecisionSection,
    InscripcionPersonalEducationSection,
    InscripcionPersonalWorkSection,
    ReactiveFormsModule,
  ],
  templateUrl: './inscripcion-personal-step.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionPersonalStep {
  protected readonly facade = inject(InscripcionFlowFacade);
}
