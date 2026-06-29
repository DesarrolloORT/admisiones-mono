import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtCheckboxModule,
  OrtIconModule,
  OrtSnackbarModule,
} from '@desarrolloort/components';

import { ScholarshipVariant } from '../scholarship-personal-step/scholarship-personal-step';

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

  readonly variant = input.required<ScholarshipVariant>();
}
