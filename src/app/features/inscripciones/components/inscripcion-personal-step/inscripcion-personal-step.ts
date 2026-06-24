import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, forwardRef, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtAccordionModule,
  OrtButtonModule,
  OrtCheckboxModule,
  OrtDatePickerModule,
  OrtFileUploader,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtRadioModule,
  OrtRatingModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { InscripcionSurveyFacade } from '../../facades/inscripcion-survey';

@Component({
  selector: 'app-inscripcion-personal-step',
  imports: [
    OrtAccordionModule,
    OrtButtonModule,
    OrtCheckboxModule,
    OrtDatePickerModule,
    forwardRef(() => OrtFileUploader),
    OrtFormFieldModule,
    OrtIconModule,
    OrtInputModule,
    OrtRadioModule,
    OrtRatingModule,
    OrtSelectModule,
    ReactiveFormsModule,
    DatePipe,
  ],
  templateUrl: './inscripcion-personal-step.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionPersonalStep {
  protected readonly facade = inject(InscripcionSurveyFacade);

  protected onSubmit(event: SubmitEvent): void {
    event.preventDefault();
    this.facade.continue();
  }
}
