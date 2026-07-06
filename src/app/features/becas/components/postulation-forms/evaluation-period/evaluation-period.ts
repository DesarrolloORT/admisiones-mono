import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtError, OrtRadioModule } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipProposalFacade } from '../../../facades/scholarship-proposal';

@Component({
  selector: 'app-evaluation-period',
  imports: [ReactiveFormsModule, OrtRadioModule, OrtError],
  templateUrl: './evaluation-period.html',
  styleUrl: '../../../pages/scholarship-process/scholarship-process.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EvaluationPeriod {
  protected readonly facade = inject(ScholarshipProposalFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly evaluationDateControl = this.facade.evaluationDateControl;

  protected readonly radioGroupOrientation = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'vertical' : 'horizontal';
  });
}
