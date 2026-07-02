import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrtFormFieldModule, OrtRadioModule } from '@desarrolloort/components';

@Component({
  selector: 'app-personal-data',
  imports: [ReactiveFormsModule, OrtFormFieldModule, OrtRadioModule],
  templateUrl: './personal-data.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonalData {
  protected readonly submitted = signal(false);
  protected readonly personalDataForm = new FormGroup({
    attendanceMode: new FormControl<string | null>(null, Validators.required),
  });
  protected readonly attendanceModeControl = this.personalDataForm.controls.attendanceMode;

  public validateAndMarkTouched(): boolean {
    this.submitted.set(true);
    this.personalDataForm.markAllAsTouched();

    return this.personalDataForm.valid;
  }
}
