import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { OrtButtonModule } from '@desarrolloort/components';

import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';

@Component({
  selector: 'app-inscripcion-regulation-reader',
  imports: [OrtButtonModule],
  templateUrl: './inscripcion-regulation-reader.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionRegulationReader {
  protected readonly facade = inject(InscripcionFlowFacade);
}
