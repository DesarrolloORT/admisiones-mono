import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule, OrtFormFieldModule } from '@desarrolloort/components';
import { AcademicProposalSelect } from 'src/app/features/catalogs/components/academic-proposal-select/academic-proposal-select';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { InscripcionProposalFacade } from '../../../facades/inscription-proposal';

@Component({
  selector: 'app-inscription-academic-step',
  imports: [
    AcademicProposalSelect,
    ErrorAlert,
    OrtButtonModule,
    OrtFormFieldModule,
    ReactiveFormsModule,
  ],
  templateUrl: './inscription-academic-step.html',
  styleUrl: '../../../pages/inscription/inscription.scss',
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
