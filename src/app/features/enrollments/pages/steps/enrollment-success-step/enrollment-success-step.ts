import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  getBankSvg,
  OrtButtonModule,
  OrtCardModule,
  OrtIconModule,
  OrtStatusIconModule,
} from '@desarrolloort/components';

import { EnrollmentPaymentFacade } from '../../../facades/enrollment-payment';

@Component({
  selector: 'app-enrollment-success-step',
  imports: [OrtButtonModule, OrtCardModule, OrtIconModule, RouterLink, OrtStatusIconModule],
  templateUrl: './enrollment-success-step.html',
  styleUrls: ['../../layout.scss', '../enrollment-result-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentSuccessStep {
  protected readonly getBankSvg = getBankSvg;
  protected readonly facade = inject(EnrollmentPaymentFacade);
}
