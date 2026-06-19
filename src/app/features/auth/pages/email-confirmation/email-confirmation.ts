import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import {
  EmailConfirmationData,
  isEmailConfirmationData,
  REGISTER_EMAIL_CONFIRMATION,
  toTwoFactorConfirmationState,
  TwoFactorConfirmationState,
} from '../../models/email-confirmation';

@Component({
  selector: 'app-email-confirmation',
  imports: [OrtButtonModule, OrtIconModule],
  templateUrl: './email-confirmation.html',
  styleUrl: './email-confirmation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailConfirmation {
  private readonly document = inject(DOCUMENT);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly confirmation = this.getConfirmation();
  private readonly twoFactorState = this.getTwoFactorState();

  constructor() {
    if (this.confirmation.requiresTwoFactorState && !this.twoFactorState) {
      void this.router.navigateByUrl('/iniciar-sesion');
    }
  }

  protected continue(): void {
    if (this.confirmation.requiresTwoFactorState) {
      if (!this.twoFactorState) {
        void this.router.navigateByUrl('/iniciar-sesion');
        return;
      }

      void this.router.navigateByUrl(this.confirmation.actionRoute, {
        state: this.twoFactorState,
      });
      return;
    }

    void this.router.navigateByUrl(this.confirmation.actionRoute);
  }

  private getConfirmation(): EmailConfirmationData {
    const confirmation = this.route.snapshot.data['confirmation'];

    return isEmailConfirmationData(confirmation) ? confirmation : REGISTER_EMAIL_CONFIRMATION;
  }

  private getTwoFactorState(): TwoFactorConfirmationState | null {
    const navigation = this.router.getCurrentNavigation();

    return toTwoFactorConfirmationState(
      navigation?.extras.state ?? this.document.defaultView?.history.state
    );
  }
}
