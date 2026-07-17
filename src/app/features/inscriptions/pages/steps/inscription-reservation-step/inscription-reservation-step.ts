import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtIconModule,
  OrtStatusIconModule,
} from '@desarrolloort/components';

import { InscripcionPaymentFacade } from '../../../facades/inscription-payment';

@Component({
  selector: 'app-inscription-reservation-step',
  imports: [OrtButtonModule, OrtCardModule, OrtIconModule, RouterLink, OrtStatusIconModule],
  templateUrl: './inscription-reservation-step.html',
  styleUrls: ['../../layout.scss', '../inscription-result-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionReservationStep {
  protected readonly facade = inject(InscripcionPaymentFacade);
}
