import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtError,
  OrtFormFieldModule,
  OrtInputModule,
  OrtRadioModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipProposalFacade } from '../../../../../facades/scholarship-proposal';

@Component({
  selector: 'app-scholarship-inscription-section',
  imports: [ReactiveFormsModule, OrtFormFieldModule, OrtInputModule, OrtRadioModule, OrtError],
  templateUrl: './scholarship-inscription-section.html',
  styleUrl: '../../../../../pages/scholarship-process/scholarship-process.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipInscriptionSection {
  protected readonly facade = inject(ScholarshipProposalFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly inscriptionSection = this.facade.inscriptionSection;
  protected readonly selectionControl = this.facade.selectionControl;
  protected readonly applicationModeControl = this.facade.applicationModeControl;

  protected readonly radioGroupOrientation = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'vertical' : 'horizontal';
  });

  protected readonly radioGroupIndicatorPosition = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'right' : 'left';
  });
}
