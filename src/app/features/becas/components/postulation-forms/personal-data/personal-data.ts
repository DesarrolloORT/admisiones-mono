import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtRadioModule } from '@desarrolloort/components';

import { ScholarshipPersonalFacade } from '../../../facades/scholarship-personal';

@Component({
  selector: 'app-personal-data',
  imports: [ReactiveFormsModule, OrtFormFieldModule, OrtRadioModule],
  templateUrl: './personal-data.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonalData {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  protected readonly personalDataForm = this.facade.personalDataForm;
  protected readonly attendanceModeControl = this.personalDataForm.controls.attendanceMode;
}
