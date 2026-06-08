import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtIconModule, OrtSelectModule } from '@desarrolloort/components';

import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';

@Component({
  selector: 'app-inscripcion-personal-decision-section',
  imports: [OrtFormFieldModule, OrtIconModule, OrtSelectModule, ReactiveFormsModule],
  templateUrl: './inscripcion-personal-decision-section.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionPersonalDecisionSection {
  protected readonly facade = inject(InscripcionFlowFacade);
}
