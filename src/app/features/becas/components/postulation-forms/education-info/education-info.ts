import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  OrtError,
  OrtFileUploaderChange,
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtInputModule,
} from '@desarrolloort/components';

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
  protected readonly submitted = signal(false);
  protected readonly educationInfoForm = new FormGroup({
    averageSecondYear: new FormControl<string | null>(null, Validators.required),
    averageThirdYear: new FormControl<string | null>(null, Validators.required),
    certificateFile: new FormControl<boolean>(false, Validators.requiredTrue),
  });
  protected readonly averageSecondYearControl = this.educationInfoForm.controls.averageSecondYear;
  protected readonly averageThirdYearControl = this.educationInfoForm.controls.averageThirdYear;
  protected readonly certificateFileControl = this.educationInfoForm.controls.certificateFile;

  public onCertificateFilesChanged(change: OrtFileUploaderChange): void {
    const selectedFile = change.value.find(file => file.isValid)?.file ?? null;
    this.certificateFileControl.setValue(Boolean(selectedFile));
    this.certificateFileControl.markAsTouched();
  }

  public validateAndMarkTouched(): boolean {
    this.submitted.set(true);
    this.educationInfoForm.markAllAsTouched();

    return this.educationInfoForm.valid;
  }
}
