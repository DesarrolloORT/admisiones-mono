import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtRadioModule } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../../../../facades/scholarship-personal';

@Component({
  selector: 'app-scholarship-personal-data-section',
  imports: [ReactiveFormsModule, OrtFormFieldModule, OrtRadioModule],
  templateUrl: './scholarship-personal-data-section.html',
  styleUrl: '../../../../scholarship-process.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipPersonalDataSection {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly personalDataForm = this.facade.personalDataForm;
  protected readonly attendanceModeControl = this.personalDataForm.controls.attendanceMode;

  protected readonly radioGroupOrientation = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'vertical' : 'horizontal';
  });

  protected readonly radioGroupIndicatorPosition = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'right' : 'left';
  });
}
