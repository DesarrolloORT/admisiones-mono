import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtError,
  OrtFileUploaderChange,
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtInputModule,
} from '@desarrolloort/components';

import { ScholarshipPersonalFacade } from '../../../facades/scholarship-personal';

@Component({
  selector: 'app-education-info',
  imports: [
    OrtFileUploaderModule,
    OrtInputModule,
    OrtFormFieldModule,
    OrtError,
    ReactiveFormsModule,
  ],
  templateUrl: './education-info.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EducationInfo {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  protected readonly educationInfoForm = this.facade.educationInfoForm;
  protected readonly averageSecondYearControl = this.educationInfoForm.controls.averageSecondYear;
  protected readonly averageThirdYearControl = this.educationInfoForm.controls.averageThirdYear;
  protected readonly certificateFileControl = this.educationInfoForm.controls.certificateFile;

  public onCertificateFilesChanged(change: OrtFileUploaderChange): void {
    this.facade.setFileFlag(this.certificateFileControl, change);
  }
}
