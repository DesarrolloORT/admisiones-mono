import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtRadioModule,
} from '@desarrolloort/components';

import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';
import { InscripcionDialog } from '../inscripcion-dialog/inscripcion-dialog';

@Component({
  selector: 'app-inscripcion-confirmation-step',
  imports: [
    InscripcionDialog,
    OrtButtonModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtRadioModule,
    ReactiveFormsModule,
  ],
  templateUrl: './inscripcion-confirmation-step.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionConfirmationStep {
  protected readonly facade = inject(InscripcionFlowFacade);
}
