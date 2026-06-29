import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule, OrtFormFieldModule } from '@desarrolloort/components';

import { AcademicProposalSelect } from '../../../catalogs/components/academic-proposal-select/academic-proposal-select';
import { InscripcionProposalFacade } from '../../facades/inscripcion-proposal';
import { InscripcionErrorAlert } from '../inscripcion-error-alert/inscripcion-error-alert';

@Component({
  selector: 'app-inscripcion-academic-step',
  imports: [
    AcademicProposalSelect,
    InscripcionErrorAlert,
    OrtButtonModule,
    OrtFormFieldModule,
    ReactiveFormsModule,
  ],
  templateUrl: './inscripcion-academic-step.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionAcademicStep {
  protected readonly facade = inject(InscripcionProposalFacade);

  protected onFormEnter(event: Event): void {
    if (event.target instanceof HTMLInputElement && event.target.type === 'radio') {
      event.preventDefault();
    }
  }
}
