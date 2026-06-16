import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtIconButtonComponent, OrtIconModule } from "@desarrolloort/components";

import { MiBeca } from '../../models/mi-beca';
import { DashboardActionCard } from "../dashboard-action-card/dashboard-action-card";
import { DashboardCard } from "../dashboard-card/dashboard-card";

@Component({
  selector: 'app-dashboard-scholarships-section',
  imports: [OrtIconButtonComponent,
    OrtIconModule, DashboardCard, DashboardActionCard],
  templateUrl: './dashboard-scholarships-section.html',
  styleUrl: './dashboard-scholarships-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardScholarshipsSection {
   becas = input.required<MiBeca[]>();
  singleRow = input.required<boolean>();

  hasBecas = computed(() => this.becas().length > 0);

  isSingle = computed(() => this.becas().length === 1);

  isSingleLayout = computed(() => {
    return this.isSingle() && !this.singleRow();
  });
}
