import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtRadioModule, OrtRatingModule } from '@desarrolloort/components';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';

import { EnrollmentSurveyFacade } from '../../../../../facades/enrollment-survey';

@Component({
  selector: 'app-enrollment-ort-experience-section',
  imports: [OrtRadioModule, OrtRatingModule, ReactiveFormsModule, ResponsiveSelect],
  templateUrl: './enrollment-ort-experience-section.html',
  styleUrl: '../../../../layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentOrtExperienceSection {
  protected readonly facade = inject(EnrollmentSurveyFacade);
  public readonly orientation = input.required<'vertical' | 'horizontal'>();
}
