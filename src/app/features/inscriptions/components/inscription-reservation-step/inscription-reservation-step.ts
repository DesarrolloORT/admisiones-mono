import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtCardModule, OrtIconModule } from '@desarrolloort/components';

import { InscripcionPaymentFacade } from '../../facades/inscription-payment';

@Component({
  selector: 'app-inscription-reservation-step',
  imports: [OrtButtonModule, OrtCardModule, OrtIconModule, RouterLink],
  templateUrl: './inscription-reservation-step.html',
  styleUrls: ['../../pages/inscription/inscription.scss', '../inscription-result-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionReservationStep {
  protected readonly facade = inject(InscripcionPaymentFacade);
}
