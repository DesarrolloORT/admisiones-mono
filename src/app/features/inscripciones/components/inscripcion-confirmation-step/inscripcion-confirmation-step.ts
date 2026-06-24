import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtRadioModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { InscripcionPaymentFacade } from '../../facades/inscripcion-payment';
import { InscripcionDialog } from '../inscripcion-dialog/inscripcion-dialog';

@Component({
  selector: 'app-inscripcion-confirmation-step',
  imports: [
    InscripcionDialog,
    OrtButtonModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtRadioModule,
    OrtSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './inscripcion-confirmation-step.html',
  styleUrls: ['../../pages/inscripcion/inscripcion.scss', './inscripcion-confirmation-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionConfirmationStep {
  protected readonly facade = inject(InscripcionPaymentFacade);
}
