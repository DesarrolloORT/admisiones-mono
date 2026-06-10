import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtButtonModule } from '@desarrolloort/components';

interface ActionConfig {
  type: 'primary' | 'secondary' | 'text';
  label: string;
}

const ACTION_MAP: Record<string, ActionConfig> = {
  'En proceso': { type: 'primary', label: 'Continuar inscripción' },
  'Pendiente': { type: 'secondary', label: 'Ver instrucciones de pago' },
  'Confirmada': { type: 'secondary', label: 'Ver detalle' },
  'Cancelada': { type: 'secondary', label: 'Reactivar inscripción' },
  'A la espera': { type: 'text', label: 'El coordinador académico de la carrera se pondrá en contacto contigo.' },
};

const DEFAULT_ACTION: ActionConfig = { type: 'secondary', label: 'Ver detalle' };

@Component({
  selector: 'app-dashboard-quick-actions',
  imports: [OrtButtonModule],
  templateUrl: './dashboard-quick-actions.html',
  styleUrl: './dashboard-quick-actions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardQuickActions {
  readonly status = input.required<string>();
   readonly careerName = input.required<string>();

  protected readonly action = computed<ActionConfig>(() => ACTION_MAP[this.status()] ?? DEFAULT_ACTION);
  protected readonly ariaLabel = computed(() => `${this.action().label} - ${this.careerName()}`);
}

