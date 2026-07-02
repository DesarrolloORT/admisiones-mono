import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrtError, OrtRadioModule } from '@desarrolloort/components';

@Component({
  selector: 'app-work-history',
  imports: [OrtRadioModule, OrtError, ReactiveFormsModule],
  templateUrl: './work-history.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkHistory {
  protected readonly submitted = signal(false);
  protected readonly workHistoryForm = new FormGroup({
    workHistory: new FormControl<string | null>(null, Validators.required),
  });
  protected readonly workHistoryControl = this.workHistoryForm.controls.workHistory;

  public validateAndMarkTouched(): boolean {
    this.submitted.set(true);
    this.workHistoryForm.markAllAsTouched();

    return this.workHistoryForm.valid;
  }
}
