import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import { EnrollmentSummary } from '../../models/enrollment-summary';
import { DashboardActionCard } from '../dashboard-action-card/dashboard-action-card';
import { DashboardCard } from '../dashboard-card/dashboard-card';

@Component({
  selector: 'app-dashboard-enrollments-section',
  imports: [DashboardActionCard, DashboardCard, OrtButtonModule, OrtIconModule, RouterLink],
  templateUrl: './dashboard-enrollments-section.html',
  styleUrl: './dashboard-enrollments-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardEnrollmentsSection {
  readonly enrollments = input.required<EnrollmentSummary[]>();
  readonly singleRow = input.required<boolean>();
}
