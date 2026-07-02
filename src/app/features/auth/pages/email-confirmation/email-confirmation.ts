import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import {
  EmailConfirmationData,
  isEmailConfirmationData,
  REGISTER_EMAIL_CONFIRMATION,
} from '../../models/email-confirmation';

@Component({
  selector: 'app-email-confirmation',
  imports: [OrtButtonModule, OrtIconModule],
  templateUrl: './email-confirmation.html',
  styleUrl: './email-confirmation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailConfirmation {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly confirmation = this.getConfirmation();

  protected continue(): void {
    void this.router.navigateByUrl(this.confirmation.actionRoute);
  }

  private getConfirmation(): EmailConfirmationData {
    const confirmation = this.route.snapshot.data['confirmation'];

    return isEmailConfirmationData(confirmation) ? confirmation : REGISTER_EMAIL_CONFIRMATION;
  }
}
