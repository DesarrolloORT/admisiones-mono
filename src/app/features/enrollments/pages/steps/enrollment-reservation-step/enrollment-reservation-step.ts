import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtIconModule,
  OrtStatusIconModule,
} from '@desarrolloort/components';

import { EnrollmentPaymentFacade } from '../../../facades/enrollment-payment';

@Component({
  selector: 'app-enrollment-reservation-step',
  imports: [OrtButtonModule, OrtCardModule, OrtIconModule, RouterLink, OrtStatusIconModule],
  templateUrl: './enrollment-reservation-step.html',
  styleUrls: ['../../layout.scss', '../enrollment-result-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentReservationStep {
  protected readonly facade = inject(EnrollmentPaymentFacade);
}
