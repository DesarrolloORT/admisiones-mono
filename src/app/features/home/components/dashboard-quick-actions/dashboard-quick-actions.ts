import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

type CardType = 'careers' | 'scholarships';

interface ActionConfig {
  type: 'primary' | 'secondary' | 'text';
  label: string;
  icon?: string;
}

const CAREER_ACTIONS: Record<string, ActionConfig> = {
  'En proceso': { type: 'primary', label: 'Continuar inscripción' },
  'Pendiente': { type: 'secondary', label: 'Ver instrucciones de pago' },
  'Pago pendiente': { type: 'secondary', label: 'Ver instrucciones de pago' },
  'Confirmada': { type: 'secondary', label: 'Ver detalle' },
  'Cancelada': { type: 'secondary', label: 'Reactivar inscripción' },
  'A la espera': { type: 'text', label: 'El coordinador académico de la carrera se pondrá en contacto contigo.' },
};

const SCHOLARSHIP_ACTIONS: Record<string, ActionConfig> = {
  'En proceso': { type: 'primary', label: 'Continuar postulación' },
  'Consulta': { type: 'secondary', label: 'Consultar' },
  'Estudio': { type: 'primary', label: 'Descargar material de estudio', icon: 'download' },
};

const DEFAULT_ACTION: ActionConfig = { type: 'secondary', label: 'Ver detalle' };

@Component({
  selector: 'app-dashboard-quick-actions',
  imports: [OrtButtonModule, OrtIconModule],
  templateUrl: './dashboard-quick-actions.html',
  styleUrl: './dashboard-quick-actions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardQuickActions {
  readonly status = input.required<string>();
  readonly careerName = input.required<string>();
  readonly cardType = input<CardType>('careers');

  protected readonly action = computed<ActionConfig>(() => {
    const map = this.cardType() === 'scholarships' ? SCHOLARSHIP_ACTIONS : CAREER_ACTIONS;
    return map[this.status()] ?? DEFAULT_ACTION;
  });
  protected readonly ariaLabel = computed(() => `${this.action().label} - ${this.careerName()}`);
}

