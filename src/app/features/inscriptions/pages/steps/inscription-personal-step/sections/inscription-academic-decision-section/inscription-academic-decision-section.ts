import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtInputModule, OrtRadioModule } from '@desarrolloort/components';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';

@Component({
  selector: 'app-inscription-academic-decision-section',
  imports: [
    OrtFormFieldModule,
    OrtInputModule,
    OrtRadioModule,
    ReactiveFormsModule,
    ResponsiveSelect,
  ],
  templateUrl: './inscription-academic-decision-section.html',
  styleUrl: '../../../../layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionAcademicDecisionSection {
  protected readonly facade = inject(InscripcionSurveyFacade);
  public readonly orientation = input.required<'vertical' | 'horizontal'>();
}
