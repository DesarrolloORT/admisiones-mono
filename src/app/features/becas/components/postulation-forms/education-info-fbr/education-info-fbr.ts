import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtError,
  OrtFileUploaderChange,
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtInputModule,
  OrtRadioModule,
} from '@desarrolloort/components';

import { ScholarshipPersonalFacade } from '../../../facades/scholarship-personal';

@Component({
  selector: 'app-education-info-fbr',
  imports: [
    OrtFormFieldModule,
    OrtFileUploaderModule,
    OrtRadioModule,
    OrtInputModule,
    OrtError,
    ReactiveFormsModule,
  ],
  templateUrl: './education-info-fbr.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EducationInfoFbr {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  protected readonly educationInfoFbrForm = this.facade.educationInfoFbrForm;
  protected readonly schoolLocationControl = this.educationInfoFbrForm.controls.schoolLocation;
  protected readonly lastYearControl = this.educationInfoFbrForm.controls.lastYear;
  protected readonly universityLocationControl =
    this.educationInfoFbrForm.controls.universityLocation;
  protected readonly careerControl = this.educationInfoFbrForm.controls.career;
  protected readonly approvedSubjectsControl = this.educationInfoFbrForm.controls.approvedSubjects;
  protected readonly totalSubjectsControl = this.educationInfoFbrForm.controls.totalSubjects;
  protected readonly averageControl = this.educationInfoFbrForm.controls.average;
  protected readonly averageRevalidationControl =
    this.educationInfoFbrForm.controls.averageRevalidation;
  protected readonly revalidationFormFileControl =
    this.educationInfoFbrForm.controls.revalidationFormFile;

  public onRevalidationFormFilesChanged(change: OrtFileUploaderChange): void {
    this.facade.setFileFlag(this.revalidationFormFileControl, change);
  }
}
