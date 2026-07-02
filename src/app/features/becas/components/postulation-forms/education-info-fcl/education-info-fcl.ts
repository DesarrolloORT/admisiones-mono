import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrtError, OrtRadioModule } from '@desarrolloort/components';

@Component({
  selector: 'app-education-info-fcl',
  imports: [OrtRadioModule, OrtError, ReactiveFormsModule],
  templateUrl: './education-info-fcl.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EducationInfoFcl {
  protected readonly submitted = signal(false);
  protected readonly educationInfoFclForm = new FormGroup({
    otherStudies: new FormControl<string | null>(null, Validators.required),
  });
  protected readonly otherStudiesControl = this.educationInfoFclForm.controls.otherStudies;

  public validateAndMarkTouched(): boolean {
    this.submitted.set(true);
    this.educationInfoFclForm.markAllAsTouched();

    return this.educationInfoFclForm.valid;
  }
}
