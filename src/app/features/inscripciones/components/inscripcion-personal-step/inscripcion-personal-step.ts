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
  OrtSelectModule,
} from '@desarrolloort/components';

import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';

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
    OrtSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './inscripcion-personal-step.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionPersonalStep {
  protected readonly facade = inject(InscripcionFlowFacade);

  protected onSubmit(event: SubmitEvent): void {
    event.preventDefault();
    this.facade.continue();
  }
}
