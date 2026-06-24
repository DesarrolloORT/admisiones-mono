import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtRadioModule,
  OrtSelectModule,
  OrtSpinnerModule,
} from '@desarrolloort/components';

import { InscripcionProposalFacade } from '../../facades/inscripcion-proposal';

@Component({
  selector: 'app-inscripcion-academic-step',
  imports: [
    OrtButtonModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtRadioModule,
    OrtSelectModule,
    OrtSpinnerModule,
    ReactiveFormsModule,
  ],
  templateUrl: './inscripcion-academic-step.html',
  styleUrls: ['../../pages/inscripcion/inscripcion.scss', './inscripcion-academic-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionAcademicStep {
  protected readonly facade = inject(InscripcionProposalFacade);
}
