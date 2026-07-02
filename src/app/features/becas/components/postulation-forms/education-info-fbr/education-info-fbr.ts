import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  OrtError,
  OrtFileUploaderChange,
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtInputModule,
  OrtRadioModule,
} from '@desarrolloort/components';

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
  protected readonly submitted = signal(false);
  protected readonly educationInfoFbrForm = new FormGroup({
    schoolLocation: new FormControl<string | null>(null, Validators.required),
    lastYear: new FormControl<string | null>(null, Validators.required),
    universityLocation: new FormControl<string | null>(null, Validators.required),
    career: new FormControl<string | null>(null, Validators.required),
    approvedSubjects: new FormControl<string | null>(null, Validators.required),
    totalSubjects: new FormControl<string | null>(null, Validators.required),
    average: new FormControl<string | null>(null, Validators.required),
    averageRevalidation: new FormControl<string | null>(null, Validators.required),
    revalidationFormFile: new FormControl<boolean>(false, Validators.requiredTrue),
  });
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
    const selectedFile = change.value.find(file => file.isValid)?.file ?? null;
    this.revalidationFormFileControl.setValue(Boolean(selectedFile));
    this.revalidationFormFileControl.markAsTouched();
  }

  public validateAndMarkTouched(): boolean {
    this.submitted.set(true);
    this.educationInfoFbrForm.markAllAsTouched();

    return this.educationInfoFbrForm.valid;
  }
}
