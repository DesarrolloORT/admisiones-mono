import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ExpandableStepperStep } from '@desarrolloort/components';

import { ProcessLayout } from '../../../../shared/ui/process-layout/process-layout';
import { ScholarshipCard } from '../../components/scholarship-card/scholarship-card';

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

const SCHOLARSHIPS = [
  {
    title: 'Beca de Reválidas',
    description:
      'Dirigida a estudiantes que solicitan reválida de materias cursadas en otras universidades, nacionales o extranjeras.',
    test: false,
    route: '/becas/fbr',
  },
  {
    title: 'Excelencia Académica',
    description:
      'Dirigida a estudiantes que comienzan una carrera y cuentan con un destacado desempeño académico en secundaria.',
    test: true,
    route: '',
  },
  {
    title: 'Becas Concursables',
    description:
      'Dirigidas a estudiantes que comienzan una carrera y han aprobado bachillerato o tienen exámenes de 6.º año pendientes.',
    test: true,
    route: '',
  },
  {
    title: 'Carreras Cortas / Capacitación Laboral',
    description:
      'Dirigida a estudiantes que desean cursar una carrera corta y cuentan con al menos 4.º año de secundaria aprobado.',
    test: false,
    route: '',
  },
];

@Component({
  selector: 'app-becas',
  imports: [ProcessLayout, ScholarshipCard],
  templateUrl: './becas.html',
  styleUrl: './becas.scss',

  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Becas {
  private readonly router = inject(Router);

  protected readonly steps = SCHOLARSHIP_STEPS;

  protected readonly becas = signal(SCHOLARSHIPS);

  protected inscripto = signal(false);

  protected goHome(): void {
    void this.router.navigate(['/inicio']);
  }
}
