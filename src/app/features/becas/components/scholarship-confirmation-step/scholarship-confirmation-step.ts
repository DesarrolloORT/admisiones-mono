import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtCheckboxModule,
  OrtIconModule,
  OrtSnackbarModule,
} from '@desarrolloort/components';

@Component({
  selector: 'app-scholarship-confirmation-step',
  imports: [OrtIconModule, OrtCardModule, OrtButtonModule, OrtSnackbarModule, OrtCheckboxModule],
  templateUrl: './scholarship-confirmation-step.html',
  styleUrls: ['./scholarship-confirmation-step.scss', '../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipConfirmationStep {
  readonly confirmApplication = output<void>();

  protected onConfirmApplication(): void {
    this.confirmApplication.emit();
  }
}
