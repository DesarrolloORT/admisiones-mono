import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule, OrtFormFieldModule } from '@desarrolloort/components';
import { AcademicProposalSelect } from 'src/app/features/catalogs/components/academic-proposal-select/academic-proposal-select';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { EnrollmentProposalFacade } from '../../../facades/enrollment-proposal';

@Component({
  selector: 'app-enrollment-academic-step',
  imports: [
    AcademicProposalSelect,
    ErrorAlert,
    OrtButtonModule,
    OrtFormFieldModule,
    ReactiveFormsModule,
  ],
  templateUrl: './enrollment-academic-step.html',
  styleUrl: '../../layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentAcademicStep {
  protected readonly facade = inject(EnrollmentProposalFacade);

  protected onFormEnter(event: Event): void {
    if (event.target instanceof HTMLInputElement && event.target.type === 'radio') {
      event.preventDefault();
    }
  }
}
