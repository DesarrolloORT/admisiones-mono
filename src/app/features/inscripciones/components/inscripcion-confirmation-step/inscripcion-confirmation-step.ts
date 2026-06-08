import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule, OrtIconModule, OrtRadioModule } from '@desarrolloort/components';

import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';

@Component({
  selector: 'app-inscripcion-confirmation-step',
  imports: [OrtButtonModule, OrtIconModule, OrtRadioModule, ReactiveFormsModule],
  templateUrl: './inscripcion-confirmation-step.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionConfirmationStep {
  protected readonly facade = inject(InscripcionFlowFacade);
}
