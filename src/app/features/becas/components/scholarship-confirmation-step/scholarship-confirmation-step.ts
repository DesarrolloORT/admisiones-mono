import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtCheckboxModule,
  OrtIconModule,
  OrtSnackbarModule,
} from '@desarrolloort/components';

import { ScholarshipVariant } from '../scholarship-personal-step/scholarship-personal-step';
import { TermsAndConditions } from '../terms-and-conditions/terms-and-conditions';

@Component({
  selector: 'app-scholarship-confirmation-step',
  imports: [
    OrtIconModule,
    OrtCardModule,
    OrtButtonModule,
    OrtSnackbarModule,
    OrtCheckboxModule,
    TermsAndConditions,
  ],
  templateUrl: './scholarship-confirmation-step.html',
  styleUrls: ['./scholarship-confirmation-step.scss', '../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipConfirmationStep {
  showTermsAndConditions = signal(false);
  termsAccepted = signal(false);

  openTermsAndConditions(): void {
    this.showTermsAndConditions.set(true);
  }

  acceptTermsAndConditions(): void {
    this.termsAccepted.set(true);
    this.showTermsAndConditions.set(false);
  }

  readonly confirmApplication = output<void>();

  protected onConfirmApplication(): void {
    this.confirmApplication.emit();
  }

  readonly variant = input.required<ScholarshipVariant>();
}
