import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtInputModule, OrtRadioModule } from '@desarrolloort/components';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';

import { EnrollmentSurveyFacade } from '../../../../../facades/enrollment-survey';

@Component({
  selector: 'app-enrollment-academic-decision-section',
  imports: [
    OrtFormFieldModule,
    OrtInputModule,
    OrtRadioModule,
    ReactiveFormsModule,
    ResponsiveSelect,
  ],
  templateUrl: './enrollment-academic-decision-section.html',
  styleUrl: '../../../../layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentAcademicDecisionSection {
  protected readonly facade = inject(EnrollmentSurveyFacade);
  public readonly orientation = input.required<'vertical' | 'horizontal'>();
}
