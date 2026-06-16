import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ExpandableStepperStep } from '@desarrolloort/components';

import { ProcessLayout } from '../../../../shared/ui/process-layout/process-layout';

const SCHOLARSHIP_STEPS: ExpandableStepperStep[] = [
  {
    id: 'oportunidades',
    overline: 'Paso 1',
    status: 'current',
    title: 'Inicio',
  },
  {
    id: 'postulacion',
    overline: 'Paso 2',
    status: 'pending',
    title: 'Postulación',
  },
  {
    id: 'resultado',
    overline: 'Paso 3',
    status: 'pending',
    title: 'Resultado',
  },
];

@Component({
  selector: 'app-becas',
  imports: [ProcessLayout],
  templateUrl: './becas.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Becas {
  private readonly router = inject(Router);

  protected readonly steps = SCHOLARSHIP_STEPS;

  protected goHome(): void {
    void this.router.navigate(['/inicio']);
  }
}

