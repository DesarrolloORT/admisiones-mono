import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule, OrtFormFieldModule } from '@desarrolloort/components';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { InscripcionProposalFacade } from '../../facades/inscripcion-proposal';
import { AcademicProposalSelect } from '../academic-proposal-select/academic-proposal-select';

@Component({
  selector: 'app-inscripcion-academic-step',
  imports: [
    AcademicProposalSelect,
    ErrorAlert,
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
