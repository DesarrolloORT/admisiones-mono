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
  OrtSelectModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { InscripcionSurveyFacade } from '../../facades/inscripcion-survey';
import { InscripcionErrorAlert } from '../inscripcion-error-alert/inscripcion-error-alert';

@Component({
  selector: 'app-inscripcion-personal-step',
  imports: [
    OrtAccordionModule,
    OrtButtonModule,
    OrtCheckboxModule,
    OrtDatePickerModule,
    OrtDivider,
    OrtFormFieldModule,
    OrtIconModule,
    OrtInputModule,
    InscripcionErrorAlert,
    OrtRadioModule,
    OrtRatingModule,
    OrtSelectModule,
    ReactiveFormsModule,
    DatePipe,
    OrtFileUploaderModule,
  ],
  templateUrl: './inscripcion-personal-step.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionPersonalStep {
  protected readonly facade = inject(InscripcionSurveyFacade);
  private readonly breakpointService = inject(BreakpointService);

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
