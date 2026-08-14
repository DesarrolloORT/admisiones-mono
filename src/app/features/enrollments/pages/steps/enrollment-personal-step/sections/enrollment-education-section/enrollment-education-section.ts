import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtDivider,
  OrtFormFieldModule,
  OrtInputModule,
  OrtRadioModule,
} from '@desarrolloort/components';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';

import { EnrollmentSurveyFacade } from '../../../../../facades/enrollment-survey';

@Component({
  selector: 'app-enrollment-education-section',
  imports: [
    OrtDivider,
    OrtFormFieldModule,
    OrtInputModule,
    OrtRadioModule,
    ReactiveFormsModule,
    ResponsiveSelect,
  ],
  templateUrl: './enrollment-education-section.html',
  styleUrls: ['../../../../layout.scss', '../../enrollment-personal-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentEducationSection {
  protected readonly facade = inject(EnrollmentSurveyFacade);
  public readonly orientation = input.required<'vertical' | 'horizontal'>();
}
