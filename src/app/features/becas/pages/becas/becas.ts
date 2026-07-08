import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { HomeHeader } from 'src/app/shared/ui/home-header/home-header';

import { ScholarshipCard } from '../../components/scholarship-card/scholarship-card';

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
    route: '/becas/fexa',
  },
  {
    title: 'Becas Concursables',
    description:
      'Dirigidas a estudiantes que comienzan una carrera y han aprobado bachillerato o tienen exámenes de 6.º año pendientes.',
    test: true,
    route: '/becas/fbc',
  },
  {
    title: 'Carreras Cortas / Capacitación Laboral',
    description:
      'Dirigida a estudiantes que desean cursar una carrera corta y cuentan con al menos 4.º año de secundaria aprobado.',
    test: false,
    route: '/becas/fcl',
  },
];

@Component({
  selector: 'app-becas',
  imports: [ScholarshipCard, HomeHeader],
  templateUrl: './becas.html',
  styleUrl: './becas.scss',

  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Becas {
  private readonly router = inject(Router);

  protected readonly becas = signal(SCHOLARSHIPS);

  protected inscripto = signal(true);

  private readonly breakpointService = inject(BreakpointService);

  readonly showBack = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall;
  });
}
