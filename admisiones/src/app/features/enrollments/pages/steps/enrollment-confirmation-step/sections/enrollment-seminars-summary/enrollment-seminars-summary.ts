import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { OrtIconModule } from '@desarrolloort/components';

import type { SeminarSummaryItem } from '../../../../../models/enrollment-flow';

// Actualización profesional: lista de seminarios del "Resumen de inscripción". Vive en
// su propio componente (en vez de un @if en enrollment-confirmation-step.html) para no
// sumar ramas al cyclomatic-complexity del template del paso de pago.
@Component({
  selector: 'app-enrollment-seminars-summary',
  imports: [OrtIconModule],
  templateUrl: './enrollment-seminars-summary.html',
  styleUrl: './enrollment-seminars-summary.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentSeminarsSummary {
  public readonly seminars = input<readonly SeminarSummaryItem[]>([]);
}
