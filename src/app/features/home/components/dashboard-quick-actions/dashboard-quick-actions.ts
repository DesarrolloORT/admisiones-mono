import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-dashboard-quick-actions',
  templateUrl: './dashboard-quick-actions.html',
  styleUrl: './dashboard-quick-actions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardQuickActions {}
