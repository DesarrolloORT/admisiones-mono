import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtRadioModule, OrtRatingModule } from '@desarrolloort/components';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';

@Component({
  selector: 'app-inscription-ort-experience-section',
  imports: [OrtRadioModule, OrtRatingModule, ReactiveFormsModule, ResponsiveSelect],
  templateUrl: './inscription-ort-experience-section.html',
  styleUrl: '../../../../../pages/inscription/inscription.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionOrtExperienceSection {
  protected readonly facade = inject(InscripcionSurveyFacade);
  public readonly orientation = input.required<'vertical' | 'horizontal'>();
}
