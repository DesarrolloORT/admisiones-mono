import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

type CardType = 'careers' | 'scholarships';

interface ActionConfig {
  type: 'primary' | 'secondary' | 'text';
  label: string;
  icon?: string;
}

const CAREER_ACTIONS: Record<string, ActionConfig> = {
  'En proceso': { type: 'primary', label: 'Continuar inscripción' },
  Pendiente: { type: 'secondary', label: 'Ver instrucciones de pago' },
  'Pago pendiente': { type: 'secondary', label: 'Ver instrucciones de pago' },
  Confirmada: { type: 'secondary', label: 'Ver detalle' },
  Cancelada: { type: 'secondary', label: 'Reactivar inscripción' },
  'A la espera': {
    type: 'text',
    label: 'El coordinador académico de la carrera se pondrá en contacto contigo.',
  },
};

const SCHOLARSHIP_ACTIONS: Record<string, ActionConfig> = {
  'En proceso': { type: 'primary', label: 'Continuar postulación' },
  Consulta: { type: 'secondary', label: 'Consultar' },
  Estudio: { type: 'primary', label: 'Descargar material de estudio', icon: 'download' },
};

const DEFAULT_ACTION: ActionConfig = { type: 'secondary', label: 'Ver detalle' };

@Component({
  selector: 'app-dashboard-quick-actions',
  imports: [OrtButtonModule, OrtIconModule, RouterLink],
  templateUrl: './dashboard-quick-actions.html',
  styleUrl: './dashboard-quick-actions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardQuickActions {
  readonly status = input.required<string>();
  readonly careerName = input.required<string>();
  readonly cardType = input<CardType>('careers');
  readonly idProducto = input<number | null>(null);
  readonly idProceso = input<number | null>(null);

  protected readonly action = computed<ActionConfig>(() => {
    const map = this.cardType() === 'scholarships' ? SCHOLARSHIP_ACTIONS : CAREER_ACTIONS;
    return map[this.status()] ?? DEFAULT_ACTION;
  });
  protected readonly ariaLabel = computed(() => `${this.action().label} - ${this.careerName()}`);
  protected readonly resumeQueryParams = computed(() => ({
    idProducto: this.idProducto(),
    idProceso: this.idProceso(),
  }));
  // Retoma la inscripción en el flujo común; el detalle resuelto posiciona el paso.
  protected readonly resumesFlow = computed(
    () =>
      this.cardType() === 'careers' &&
      ['En proceso', 'Pago pendiente', 'Confirmada'].includes(this.status()) &&
      Number.isSafeInteger(this.idProducto()) &&
      Number.isSafeInteger(this.idProceso()) &&
      (this.idProducto() ?? 0) > 0 &&
      (this.idProceso() ?? 0) > 0
  );
}
