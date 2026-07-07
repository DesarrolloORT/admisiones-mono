import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtAccordionModule,
  OrtButtonModule,
  OrtCheckboxModule,
  OrtDatePickerModule,
  OrtDivider,
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtRadioModule,
  OrtRatingModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';

import { InscripcionSurveyFacade } from '../../facades/inscription-survey';

@Component({
  selector: 'app-inscription-personal-step',
  imports: [
    DatePipe,
    ErrorAlert,
    OrtAccordionModule,
    OrtButtonModule,
    OrtCheckboxModule,
    OrtDatePickerModule,
    OrtDivider,
    OrtFileUploaderModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtInputModule,
    OrtRadioModule,
    OrtRatingModule,
    ReactiveFormsModule,
    ResponsiveSelect,
  ],
  templateUrl: './inscription-personal-step.html',
  styleUrls: ['../../pages/inscription/inscription.scss', './inscription-personal-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionPersonalStep {
  protected readonly facade = inject(InscripcionSurveyFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly divider = true;

  protected readonly radioGroupOrientation = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'vertical' : 'horizontal';
  });

  protected onFormEnter(event: Event): void {
    if (event.target instanceof HTMLInputElement && event.target.type === 'radio') {
      event.preventDefault();
    }
  }

  protected onSubmit(event: SubmitEvent): void {
    event.preventDefault();
    this.facade.continue();
  }

  protected openRegulationReader(event: Event): void {
    event.preventDefault();
    this.facade.openRegulationReader();
  }
}
