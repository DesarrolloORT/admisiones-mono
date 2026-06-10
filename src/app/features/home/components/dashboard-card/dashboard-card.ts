import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { OrtIconModule } from '@desarrolloort/components';

import { MiInscripcion } from '../../../home/models/mi-inscripcion';
import { DashboardCareerStatusChip } from '../dashboard-career-status-chip/dashboard-career-status-chip';
import { DashboardQuickActions } from '../dashboard-quick-actions/dashboard-quick-actions';

@Component({
  selector: 'app-dashboard-card',
  imports: [OrtIconModule, DashboardCareerStatusChip, DashboardQuickActions],
  templateUrl: './dashboard-card.html',
  styleUrl: './dashboard-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCard {
  readonly inscripcion = input.required<MiInscripcion>();
}
