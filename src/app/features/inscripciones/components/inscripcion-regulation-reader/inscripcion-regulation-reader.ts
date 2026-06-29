import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { OrtButtonModule, OrtCardModule } from '@desarrolloort/components';

import { InscripcionSurveyFacade } from '../../facades/inscripcion-survey';

@Component({
  selector: 'app-inscripcion-regulation-reader',
  imports: [OrtButtonModule, OrtCardModule],
  templateUrl: './inscripcion-regulation-reader.html',
  styleUrl: '../../pages/inscripcion/inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionRegulationReader {
  protected readonly facade = inject(InscripcionSurveyFacade);
}
