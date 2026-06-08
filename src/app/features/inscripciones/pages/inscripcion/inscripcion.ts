import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { InscripcionAcademicStep } from '../../components/inscripcion-academic-step/inscripcion-academic-step';
import { InscripcionConfirmationStep } from '../../components/inscripcion-confirmation-step/inscripcion-confirmation-step';
import { InscripcionPersonalStep } from '../../components/inscripcion-personal-step/inscripcion-personal-step';
import { InscripcionShell } from '../../components/inscripcion-shell/inscripcion-shell';
import { InscripcionSuccessStep } from '../../components/inscripcion-success-step/inscripcion-success-step';
import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';

@Component({
  selector: 'app-inscripcion',
  imports: [
    InscripcionAcademicStep,
    InscripcionConfirmationStep,
    InscripcionPersonalStep,
    InscripcionShell,
    InscripcionSuccessStep,
  ],
  providers: [InscripcionFlowFacade],
  templateUrl: './inscripcion.html',
  styleUrl: './inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inscripcion {
  protected readonly facade = inject(InscripcionFlowFacade);
}
