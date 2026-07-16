import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import { HomeService } from '../../services/home';

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
  'Dada de baja': { type: 'secondary', label: 'Reactivar inscripción' },
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
  private readonly homeService = inject(HomeService);
  private readonly router = inject(Router);

  readonly status = input.required<string>();
  readonly careerName = input.required<string>();
  readonly cardType = input<CardType>('careers');
  readonly idProducto = input<number | null>(null);
  readonly idProceso = input<number | null>(null);
  readonly idInscripto = input<number | null>(null);
  private readonly isReactivating = signal(false);

  protected readonly action = computed<ActionConfig>(() => {
    const map = this.cardType() === 'scholarships' ? SCHOLARSHIP_ACTIONS : CAREER_ACTIONS;
    return map[this.status()] ?? DEFAULT_ACTION;
  });
  protected readonly ariaLabel = computed(() => `${this.action().label} - ${this.careerName()}`);
  protected readonly resumeQueryParams = computed(() => ({
    idProducto: this.idProducto(),
    idProceso: this.idProceso(),
  }));

  protected readonly resumesFlow = computed(
    () =>
      this.cardType() === 'careers' &&
      ['En proceso', 'Pendiente', 'Pago pendiente', 'Confirmada'].includes(this.status()) &&
      Number.isSafeInteger(this.idProducto()) &&
      Number.isSafeInteger(this.idProceso()) &&
      (this.idProducto() ?? 0) > 0 &&
      (this.idProceso() ?? 0) > 0
  );
  protected readonly reactivatesFlow = computed(
    () =>
      this.cardType() === 'careers' &&
      this.status() === 'Dada de baja' &&
      Number.isSafeInteger(this.idInscripto()) &&
      (this.idInscripto() ?? 0) > 0 &&
      Number.isSafeInteger(this.idProducto()) &&
      (this.idProducto() ?? 0) > 0 &&
      Number.isSafeInteger(this.idProceso()) &&
      (this.idProceso() ?? 0) > 0
  );

  protected reactivate(): void {
    const idInscripto = this.idInscripto();
    if (this.isReactivating() || !idInscripto) return;
    this.isReactivating.set(true);
    this.homeService.reactivarInscripcion(idInscripto).subscribe({
      next: () =>
        void this.router.navigate(['/inscripciones'], { queryParams: this.resumeQueryParams() }),
      error: () => this.isReactivating.set(false),
    });
  }
}
