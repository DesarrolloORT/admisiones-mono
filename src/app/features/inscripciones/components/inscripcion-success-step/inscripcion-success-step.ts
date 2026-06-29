import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtCardModule, OrtIconModule } from '@desarrolloort/components';

import { InscripcionPaymentFacade } from '../../facades/inscripcion-payment';

@Component({
  selector: 'app-inscripcion-success-step',
  imports: [OrtButtonModule, OrtCardModule, OrtIconModule, RouterLink],
  templateUrl: './inscripcion-success-step.html',
  styleUrls: ['../../pages/inscripcion/inscripcion.scss', '../inscripcion-result-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionSuccessStep {
  protected readonly facade = inject(InscripcionPaymentFacade);
}
