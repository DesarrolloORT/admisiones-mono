import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { OrtButtonModule, OrtCardModule } from '@desarrolloort/components';

import { InscripcionSurveyFacade } from '../../facades/inscription-survey';

@Component({
  selector: 'app-inscription-regulation-reader',
  imports: [OrtButtonModule, OrtCardModule],
  templateUrl: './inscription-regulation-reader.html',
  styleUrl: '../../pages/inscription/inscription.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionRegulationReader {
  protected readonly facade = inject(InscripcionSurveyFacade);
}
