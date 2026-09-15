import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtIconModule } from '@desarrolloort/components';

import { EnrollmentSummary } from '../../models/enrollment-summary';
import { ScholarshipSummary } from '../../models/scholarship-summary';
import { DashboardCardSummary } from '../dashboard-card-summary/dashboard-card-summary';
import { DashboardEnrollmentStatusChip } from '../dashboard-enrollment-status-chip/dashboard-enrollment-status-chip';
import { DashboardQuickActions } from '../dashboard-quick-actions/dashboard-quick-actions';

type CardVariant = 'enrollments' | 'scholarships';

let nextId = 0;

@Component({
  selector: 'app-dashboard-card',
  imports: [
    OrtIconModule,
    DashboardEnrollmentStatusChip,
    DashboardQuickActions,
    DashboardCardSummary,
  ],
  templateUrl: './dashboard-card.html',
  styleUrl: './dashboard-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCard {
  private readonly uid = ++nextId;

  readonly variant = input<CardVariant>('enrollments');
  readonly enrollment = input<EnrollmentSummary | null>(null);
  readonly scholarship = input<ScholarshipSummary | null>(null);

  protected readonly titleId = `dashboard-card-title-${this.uid}`;

  protected readonly icon = computed(() =>
    this.variant() === 'scholarships' ? 'workspace_premium' : 'school'
  );
  protected readonly title = computed(() =>
    this.variant() === 'scholarships'
      ? (this.scholarship()?.scholarshipName ?? '')
      : (this.enrollment()?.degreeProgramName ?? '')
  );
  protected readonly status = computed(() =>
    this.variant() === 'scholarships'
      ? (this.scholarship()?.status ?? '')
      : (this.enrollment()?.status ?? '')
  );

  protected readonly showChip = computed(() => {
    if (this.variant() === 'enrollments') return true;
    const status = this.status();
    return status === 'En proceso' || status === 'Aceptada';
  });

  protected readonly showActions = computed(() => {
    if (this.variant() === 'enrollments') return true;
    return this.status() !== 'Aceptada';
  });

  // Cantidad de anotaciones de la inscripción: cada seminario de un paquete cuenta como una;
  // sin seminarios (carrera simple) es siempre 1, la propia inscripción.
  protected readonly enrollmentsCount = computed(() => this.enrollment()?.seminars.length || 1);

  protected readonly enrollmentIds = computed(() => {
    const enrollment = this.enrollment();
    if (!enrollment) return [];

    const ids = enrollment.seminars.length
      ? enrollment.seminars.map(seminar => seminar.enrollmentId)
      : [enrollment.enrollmentId];
    return [...new Set(ids.filter(id => Number.isSafeInteger(id) && id > 0))];
  });
}
