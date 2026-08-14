import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule, OrtCheckboxModule } from '@desarrolloort/components';

import { EnrollmentSurveyFacade } from '../../../../../facades/enrollment-survey';

@Component({
  selector: 'app-enrollment-regulation-section',
  imports: [DatePipe, OrtButtonModule, OrtCheckboxModule, ReactiveFormsModule],
  templateUrl: './enrollment-regulation-section.html',
  styleUrl: '../../../../layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentRegulationSection {
  protected readonly facade = inject(EnrollmentSurveyFacade);
}
