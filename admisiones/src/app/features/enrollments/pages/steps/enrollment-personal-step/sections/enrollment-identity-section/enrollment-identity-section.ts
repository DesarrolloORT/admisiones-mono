import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtCheckboxModule,
  OrtDatePickerModule,
  OrtFileUploaderModule,
  OrtFormFieldModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { EnrollmentSurveyFacade } from '../../../../../facades/enrollment-survey';

@Component({
  selector: 'app-enrollment-identity-section',
  imports: [
    OrtCheckboxModule,
    OrtDatePickerModule,
    OrtFileUploaderModule,
    OrtFormFieldModule,
    ReactiveFormsModule,
  ],
  templateUrl: './enrollment-identity-section.html',
  styleUrls: ['../../../../layout.scss', '../../enrollment-personal-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentIdentitySection {
  protected readonly facade = inject(EnrollmentSurveyFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly fileUploaderDisplay = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'inline' : 'block';
  });
}
