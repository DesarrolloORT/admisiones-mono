import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtError, OrtRadioModule } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../facades/scholarship-personal';

@Component({
  selector: 'app-education-info-fcl',
  imports: [OrtRadioModule, OrtError, ReactiveFormsModule],
  templateUrl: './education-info-fcl.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EducationInfoFcl {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly educationInfoFclForm = this.facade.educationInfoFclForm;
  protected readonly otherStudiesControl = this.educationInfoFclForm.controls.otherStudies;

  protected readonly radioGroupOrientation = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'vertical' : 'horizontal';
  });
}
