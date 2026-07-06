import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtError, OrtRadioModule } from '@desarrolloort/components';

import { ScholarshipPersonalFacade } from '../../../facades/scholarship-personal';

@Component({
  selector: 'app-work-history',
  imports: [OrtRadioModule, OrtError, ReactiveFormsModule],
  templateUrl: './work-history.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkHistory {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  protected readonly workHistoryForm = this.facade.workHistoryForm;
  protected readonly workHistoryControl = this.workHistoryForm.controls.workHistory;
}
