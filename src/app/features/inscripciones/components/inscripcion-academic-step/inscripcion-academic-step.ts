import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule } from '@desarrolloort/components';

import { AcademicProposalSelect } from '../../../catalogs/components/academic-proposal-select/academic-proposal-select';
import { InscripcionProposalFacade } from '../../facades/inscripcion-proposal';

@Component({
  selector: 'app-inscripcion-academic-step',
  imports: [AcademicProposalSelect, OrtButtonModule, ReactiveFormsModule],
  templateUrl: './inscripcion-academic-step.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionAcademicStep {
  protected readonly facade = inject(InscripcionProposalFacade);
}
