import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ScholarshipSummary } from '../../models/scholarship-summary';

@Component({
  selector: 'app-dashboard-scholarships-section',
  templateUrl: './dashboard-scholarships-section.html',
  styleUrl: './dashboard-scholarships-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardScholarshipsSection {
  readonly scholarships = input.required<ScholarshipSummary[]>();
  readonly singleRow = input.required<boolean>();
  readonly openDialog = output<void>();
  readonly consultar = output<void>();
}
