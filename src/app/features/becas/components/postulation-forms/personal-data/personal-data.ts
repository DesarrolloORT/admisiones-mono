import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtRadioModule } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../facades/scholarship-personal';

@Component({
  selector: 'app-personal-data',
  imports: [ReactiveFormsModule, OrtFormFieldModule, OrtRadioModule],
  templateUrl: './personal-data.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonalData {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly personalDataForm = this.facade.personalDataForm;
  protected readonly attendanceModeControl = this.personalDataForm.controls.attendanceMode;

  protected readonly radioGroupOrientation = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'vertical' : 'horizontal';
  });
}
