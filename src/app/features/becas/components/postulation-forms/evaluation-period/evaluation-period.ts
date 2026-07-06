import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtError, OrtRadioModule } from '@desarrolloort/components';

import { ScholarshipProposalFacade } from '../../../facades/scholarship-proposal';

@Component({
  selector: 'app-evaluation-period',
  imports: [ReactiveFormsModule, OrtRadioModule, OrtError],
  templateUrl: './evaluation-period.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EvaluationPeriod {
  protected readonly facade = inject(ScholarshipProposalFacade);
  protected readonly evaluationDateControl = this.facade.evaluationDateControl;
}
