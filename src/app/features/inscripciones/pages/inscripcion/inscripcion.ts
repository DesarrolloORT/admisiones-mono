import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { InscripcionRadioCard } from '../../components/inscripcion-radio-card/inscripcion-radio-card';
import { InscripcionShell } from '../../components/inscripcion-shell/inscripcion-shell';
import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';

@Component({
  selector: 'app-inscripcion',
  imports: [
    InscripcionRadioCard,
    InscripcionShell,
    OrtButtonModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtSelectModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  providers: [InscripcionFlowFacade],
  templateUrl: './inscripcion.html',
  styleUrl: './inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inscripcion {
  protected readonly facade = inject(InscripcionFlowFacade);
}
