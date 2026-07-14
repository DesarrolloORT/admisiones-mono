import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtCardModule, OrtIconModule } from '@desarrolloort/components';

import { InscripcionPaymentFacade } from '../../../facades/inscription-payment';

@Component({
  selector: 'app-inscription-success-step',
  imports: [OrtButtonModule, OrtCardModule, OrtIconModule, RouterLink],
  templateUrl: './inscription-success-step.html',
  styleUrls: ['../../layout.scss', '../inscription-result-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionSuccessStep {
  protected readonly facade = inject(InscripcionPaymentFacade);
}
