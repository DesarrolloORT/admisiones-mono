import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtDivider,
  OrtFormFieldModule,
  OrtInputModule,
  OrtRadioModule,
} from '@desarrolloort/components';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';

@Component({
  selector: 'app-inscription-education-section',
  imports: [
    OrtDivider,
    OrtFormFieldModule,
    OrtInputModule,
    OrtRadioModule,
    ReactiveFormsModule,
    ResponsiveSelect,
  ],
  templateUrl: './inscription-education-section.html',
  styleUrls: [
    '../../../../../pages/inscription/inscription.scss',
    '../../inscription-personal-step.scss',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionEducationSection {
  protected readonly facade = inject(InscripcionSurveyFacade);
  public readonly orientation = input.required<'vertical' | 'horizontal'>();
}
