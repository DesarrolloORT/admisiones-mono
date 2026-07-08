import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtRadioModule } from '@desarrolloort/components';

import { InscripcionSurveyFacade } from '../../facades/inscription-survey';

@Component({
  selector: 'app-inscription-work-section',
  imports: [OrtRadioModule, ReactiveFormsModule],
  templateUrl: './inscription-work-section.html',
  styleUrl: '../../pages/inscription/inscription.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionWorkSection {
  protected readonly facade = inject(InscripcionSurveyFacade);
  public readonly orientation = input.required<'vertical' | 'horizontal'>();
}
