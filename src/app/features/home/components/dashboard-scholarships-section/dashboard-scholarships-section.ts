import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import { MiBeca } from '../../models/mi-beca';
import { DashboardActionCard } from '../dashboard-action-card/dashboard-action-card';
import { DashboardCard } from '../dashboard-card/dashboard-card';
import { ScholarshipGrantedCard } from '../scholarship-granted-card/scholarship-granted-card';

@Component({
  selector: 'app-dashboard-scholarships-section',
  imports: [
    OrtButtonModule,
    OrtIconModule,
    DashboardCard,
    DashboardActionCard,
    ScholarshipGrantedCard,
    RouterLink,
  ],
  templateUrl: './dashboard-scholarships-section.html',
  styleUrl: './dashboard-scholarships-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardScholarshipsSection {
  readonly becas = input.required<MiBeca[]>();
  readonly singleRow = input.required<boolean>();
  readonly openDialog = output<void>();
}
