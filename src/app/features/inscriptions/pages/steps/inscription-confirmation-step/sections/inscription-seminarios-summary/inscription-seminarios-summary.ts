import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { OrtIconModule } from '@desarrolloort/components';

import type { ItemSeminarioResumen } from '../../../../../models/inscription-flow';

// Actualización profesional: lista de seminarios del "Resumen de inscripción". Vive en
// su propio componente (en vez de un @if en inscription-confirmation-step.html) para no
// sumar ramas al cyclomatic-complexity del template del paso de pago.
@Component({
  selector: 'app-inscription-seminarios-summary',
  imports: [OrtIconModule],
  templateUrl: './inscription-seminarios-summary.html',
  styleUrl: './inscription-seminarios-summary.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionSeminariosSummary {
  public readonly seminarios = input<readonly ItemSeminarioResumen[]>([]);
}
