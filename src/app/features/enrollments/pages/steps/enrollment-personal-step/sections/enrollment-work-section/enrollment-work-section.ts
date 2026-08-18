import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtRadioModule } from '@desarrolloort/components';

import { EnrollmentSurveyFacade } from '../../../../../facades/enrollment-survey';

@Component({
  selector: 'app-enrollment-work-section',
  imports: [OrtRadioModule, ReactiveFormsModule],
  templateUrl: './enrollment-work-section.html',
  styleUrl: '../../../../layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentWorkSection {
  protected readonly facade = inject(EnrollmentSurveyFacade);
  public readonly orientation = input.required<'vertical' | 'horizontal'>();
}
