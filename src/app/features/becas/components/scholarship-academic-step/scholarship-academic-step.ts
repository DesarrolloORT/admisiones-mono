import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule } from '@desarrolloort/components';
import { AcademicProposalSelect } from 'src/app/features/catalogs/components/academic-proposal-select/academic-proposal-select';

import { ScholarshipProposalFacade } from '../../facades/scholarship-proposal';

@Component({
  selector: 'app-scholarship-academic-step',
  imports: [AcademicProposalSelect, OrtButtonModule, ReactiveFormsModule],
  templateUrl: './scholarship-academic-step.html',
  styleUrls: ['../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipAcademicStep {
  protected readonly facade = inject(ScholarshipProposalFacade);
}
