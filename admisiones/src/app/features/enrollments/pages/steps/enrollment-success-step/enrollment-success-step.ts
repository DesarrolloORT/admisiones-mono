import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  getBankSvg,
  OrtButtonModule,
  OrtCardModule,
  OrtIconModule,
  OrtStatusIconModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { EnrollmentPaymentFacade } from '../../../facades/enrollment-payment';

@Component({
  selector: 'app-enrollment-success-step',
  imports: [OrtButtonModule, OrtCardModule, OrtIconModule, RouterLink, OrtStatusIconModule],
  templateUrl: './enrollment-success-step.html',
  styleUrls: ['../../layout.scss', '../enrollment-result-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentSuccessStep {
  protected readonly getBankSvg = getBankSvg;
  protected readonly facade = inject(EnrollmentPaymentFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly displayedSubjects = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();
    return breakpoint.isXSmall || breakpoint.isSmall
      ? this.facade.visibleSubjects()
      : this.facade.subjects();
  });
  protected readonly canToggleSubjects = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();
    return (breakpoint.isXSmall || breakpoint.isSmall) && this.facade.canToggleSubjects();
  });
}
