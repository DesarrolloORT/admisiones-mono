import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtCheckboxModule,
  OrtDatePickerModule,
  OrtFileUploaderModule,
  OrtFormFieldModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';

@Component({
  selector: 'app-inscription-identity-section',
  imports: [
    OrtCheckboxModule,
    OrtDatePickerModule,
    OrtFileUploaderModule,
    OrtFormFieldModule,
    ReactiveFormsModule,
  ],
  templateUrl: './inscription-identity-section.html',
  styleUrls: ['../../../../layout.scss', '../../inscription-personal-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionIdentitySection {
  protected readonly facade = inject(InscripcionSurveyFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly fileUploaderDisplay = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'inline' : 'block';
  });
}
