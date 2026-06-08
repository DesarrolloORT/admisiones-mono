import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtCheckboxModule, OrtIconModule, OrtRadioModule } from '@desarrolloort/components';

import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';

@Component({
  selector: 'app-inscripcion-personal-work-section',
  imports: [OrtCheckboxModule, OrtIconModule, OrtRadioModule, ReactiveFormsModule],
  templateUrl: './inscripcion-personal-work-section.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionPersonalWorkSection {
  protected readonly facade = inject(InscripcionFlowFacade);
}
