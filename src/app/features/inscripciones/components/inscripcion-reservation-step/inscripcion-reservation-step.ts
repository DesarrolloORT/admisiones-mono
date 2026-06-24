import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import { InscripcionPaymentFacade } from '../../facades/inscripcion-payment';

@Component({
  selector: 'app-inscripcion-reservation-step',
  imports: [OrtButtonModule, OrtIconModule, RouterLink],
  templateUrl: './inscripcion-reservation-step.html',
  styleUrls: ['../../pages/inscripcion/inscripcion.scss', '../inscripcion-result-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionReservationStep {
  protected readonly facade = inject(InscripcionPaymentFacade);
}
