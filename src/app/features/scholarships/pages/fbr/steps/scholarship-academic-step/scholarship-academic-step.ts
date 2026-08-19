import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule } from '@desarrolloort/components';
import { AcademicProposalSelect } from 'src/app/features/catalogs/components/academic-proposal-select/academic-proposal-select';

import { ScholarshipProcessFacade } from '../../../../facades/scholarship-process';

@Component({
  selector: 'app-scholarship-academic-step',
  imports: [AcademicProposalSelect, OrtButtonModule, ReactiveFormsModule],
  templateUrl: './scholarship-academic-step.html',
  styleUrls: ['../../fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipAcademicStep {
  // El proceso es el unico objeto que el paso conoce: decide el avance y expone
  // la fachada de la seccion.
  protected readonly process = inject(ScholarshipProcessFacade);
  protected readonly facade = this.process.proposal;
}
