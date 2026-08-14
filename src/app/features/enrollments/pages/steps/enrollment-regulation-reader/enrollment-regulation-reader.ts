import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { OrtButtonModule, OrtCardModule } from '@desarrolloort/components';

import { EnrollmentSurveyFacade } from '../../../facades/enrollment-survey';

@Component({
  selector: 'app-enrollment-regulation-reader',
  imports: [OrtButtonModule, OrtCardModule],
  templateUrl: './enrollment-regulation-reader.html',
  styleUrl: '../../layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentRegulationReader {
  protected readonly facade = inject(EnrollmentSurveyFacade);
}
