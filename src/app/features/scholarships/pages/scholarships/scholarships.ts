import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { HomeHeader } from 'src/app/shared/ui/home-header/home-header';

import { ScholarshipCard } from '../../components/scholarship-card/scholarship-card';

const SCHOLARSHIPS = [
  {
    title: 'Beca de Reválidas',
    description:
      'Dirigida a estudiantes que solicitan reválida de materias cursadas en otras universidades, nacionales o extranjeras.',
    requiresExam: false,
    route: '/becas/fbr',
  },
  {
    title: 'Excelencia Académica',
    description:
      'Dirigida a estudiantes que comienzan una carrera y cuentan con un destacado desempeño académico en secundaria.',
    requiresExam: true,
    route: '',
  },
  {
    title: 'Becas Concursables',
    description:
      'Dirigidas a estudiantes que comienzan una carrera y han aprobado bachillerato o tienen exámenes de 6.º año pendientes.',
    requiresExam: true,
    route: '',
  },
  {
    title: 'Carreras Cortas / Capacitación Laboral',
    description:
      'Dirigida a estudiantes que desean cursar una carrera corta y cuentan con al menos 4.º año de secundaria aprobado.',
    requiresExam: false,
    route: '',
  },
];

@Component({
  selector: 'app-scholarships',
  imports: [ScholarshipCard, HomeHeader],
  templateUrl: './scholarships.html',
  styleUrl: './scholarships.scss',

  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Scholarships {
  protected readonly scholarships = signal(SCHOLARSHIPS);

  protected isEnrolled = signal(false);

  private readonly breakpointService = inject(BreakpointService);

  readonly showBack = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall;
  });
}
