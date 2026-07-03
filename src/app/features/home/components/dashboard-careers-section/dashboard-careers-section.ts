import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import { MiInscripcion } from '../../models/mi-inscripcion';
import { DashboardActionCard } from '../dashboard-action-card/dashboard-action-card';
import { DashboardCard } from '../dashboard-card/dashboard-card';

@Component({
  selector: 'app-dashboard-careers-section',
  imports: [DashboardActionCard, DashboardCard, OrtButtonModule, OrtIconModule, RouterLink],
  templateUrl: './dashboard-careers-section.html',
  styleUrl: './dashboard-careers-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCareersSection {
  readonly inscripciones = input.required<MiInscripcion[]>();
  readonly singleRow = input.required<boolean>();
}
