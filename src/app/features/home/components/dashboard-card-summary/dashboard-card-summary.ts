import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtIconModule } from '@desarrolloort/components';

type SummaryVariant = 'enrollments' | 'scholarships';

interface SummaryItem {
  icon: string;
  label: string;
  value: string;
}

@Component({
  selector: 'app-dashboard-card-summary',
  imports: [OrtIconModule],
  templateUrl: './dashboard-card-summary.html',
  styleUrl: './dashboard-card-summary.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCardSummary {
  readonly variant = input<SummaryVariant>('enrollments');
  readonly status = input<string>('');

  // DegreePrograms
  readonly startDate = input<string>('');

  // Scholarships
  readonly degreeProgramName = input<string>('');
  readonly applicationDeadline = input<string>('');
  readonly examDate = input<string>('');
  readonly examResult = input<string>('');
  readonly benefit = input<string>('');
  readonly resultsDate = input<string>('');

  protected readonly items = computed<SummaryItem[]>(() => {
    if (this.variant() === 'enrollments') {
      return this.buildEnrollmentItems();
    }
    return this.buildScholarshipItems();
  });

  private buildEnrollmentItems(): SummaryItem[] {
    return [{ icon: 'calendar_today', label: 'Comienzo', value: this.startDate() }];
  }

  private buildScholarshipItems(): SummaryItem[] {
    switch (this.status()) {
      case 'En proceso':
        return [
          { icon: 'school', label: 'Carrera', value: this.degreeProgramName() },
          { icon: 'today', label: 'Cierre de postulación', value: this.applicationDeadline() },
          { icon: 'event', label: 'Fecha de prueba', value: this.examDate() },
        ];
      case 'Consulta':
      case 'Aceptada':
        return [
          { icon: 'school', label: 'Carrera', value: this.degreeProgramName() },
          { icon: 'fact_check', label: 'Resultado de prueba', value: this.examResult() },
          { icon: 'percent', label: 'Beneficio', value: this.benefit() },
        ];
      case 'Estudio':
        return [
          { icon: 'school', label: 'Carrera', value: this.degreeProgramName() },
          { icon: 'today', label: 'Fecha de prueba', value: this.examDate() },
          { icon: 'event', label: 'Fecha de resultados', value: this.resultsDate() },
        ];
      default:
        return [{ icon: 'school', label: 'Carrera', value: this.degreeProgramName() }];
    }
  }
}
