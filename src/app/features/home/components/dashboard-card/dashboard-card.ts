import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtIconModule } from '@desarrolloort/components';

import { MiBeca } from '../../models/mi-beca';
import { MiInscripcion } from '../../models/mi-inscripcion';
import { DashboardCardSummary } from '../dashboard-card-summary/dashboard-card-summary';
import { DashboardCareerStatusChip } from '../dashboard-career-status-chip/dashboard-career-status-chip';
import { DashboardQuickActions } from '../dashboard-quick-actions/dashboard-quick-actions';

type CardVariant = 'careers' | 'scholarships';

let nextId = 0;

@Component({
  selector: 'app-dashboard-card',
  imports: [OrtIconModule, DashboardCareerStatusChip, DashboardQuickActions, DashboardCardSummary],
  templateUrl: './dashboard-card.html',
  styleUrl: './dashboard-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCard {
  private readonly uid = ++nextId;

  readonly variant = input<CardVariant>('careers');
  readonly inscripcion = input<MiInscripcion | null>(null);
  readonly beca = input<MiBeca | null>(null);

  protected readonly titleId = `dashboard-card-title-${this.uid}`;

  protected readonly icon = computed(() =>
    this.variant() === 'scholarships' ? 'workspace_premium' : 'school'
  );
  protected readonly title = computed(() =>
    this.variant() === 'scholarships'
      ? (this.beca()?.nombreBeca ?? '')
      : (this.inscripcion()?.nombreProducto ?? '')
  );
  protected readonly estado = computed(() =>
    this.variant() === 'scholarships'
      ? (this.beca()?.estado ?? '')
      : (this.inscripcion()?.estado ?? '')
  );

  protected readonly showChip = computed(() => {
    if (this.variant() === 'careers') return true;
    const estado = this.estado();
    return estado === 'En proceso' || estado === 'Aceptada';
  });

  protected readonly showActions = computed(() => {
    if (this.variant() === 'careers') return true;
    return this.estado() !== 'Aceptada';
  });

  // Cantidad de anotaciones de la inscripción: cada seminario de un paquete cuenta como una;
  // sin seminarios (carrera simple) es siempre 1, la propia inscripción.
  protected readonly enrollmentsCount = computed(() => this.inscripcion()?.seminarios.length || 1);

  protected readonly idsInscripcion = computed(() => {
    const inscripcion = this.inscripcion();
    if (!inscripcion) return [];

    const ids = inscripcion.seminarios.length
      ? inscripcion.seminarios.map(seminario => seminario.idInscripto)
      : [inscripcion.idInscripto];
    return [...new Set(ids.filter(id => Number.isSafeInteger(id) && id > 0))];
  });
}
