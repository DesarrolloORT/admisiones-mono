import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtCheckboxModule,
  OrtError,
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
    OrtError,
  ],
  templateUrl: './scholarship-confirmation-step.html',
  styleUrls: ['./scholarship-confirmation-step.scss', '../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipConfirmationStep {
  showTermsAndConditions = signal(false);
  termsAccepted = signal(false);
  readonly termsError = signal(false);

  onTermsAcceptedChange(value: string | boolean): void {
    const checked = value === true || value === 'true';

    this.termsAccepted.set(checked);

    if (checked) {
      this.termsError.set(false);
    }
  }
  openTermsAndConditions(): void {
    this.showTermsAndConditions.set(true);
  }

  acceptTermsAndConditions(): void {
    this.termsError.set(false);
    this.termsAccepted.set(true);
    this.showTermsAndConditions.set(false);
  }

  readonly confirmApplication = output<void>();

  protected onConfirmApplication(): void {
    if (!this.termsAccepted()) {
      this.termsError.set(true);
      return;
    }
    this.termsError.set(false);
    this.confirmApplication.emit();
  }

  readonly variant = input.required<ScholarshipVariant>();
}
