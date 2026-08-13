import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import type { InscripcionPreEnrollmentResponse } from '../../../inscriptions/models/inscription-flow';
import { InscriptionResumeContextStore } from '../../../inscriptions/services/inscription-resume-context';
import { Inscripciones } from '../../../inscriptions/services/inscriptions';

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
  private readonly inscriptions = inject(Inscripciones);
  private readonly router = inject(Router);
  private readonly resumeContext = inject(InscriptionResumeContextStore);

  readonly status = input.required<string>();
  readonly careerName = input.required<string>();
  readonly cardType = input<CardType>('careers');
  readonly idProducto = input<number | null>(null);
  readonly idProceso = input<number | null>(null);
  readonly idNivelProducto = input<number | null>(null);
  readonly idInscripciones = input<readonly number[]>([]);
  readonly idOfertas = input<readonly number[]>([]);
  private readonly isReactivating = signal(false);

  protected readonly action = computed<ActionConfig>(() => {
    const map = this.cardType() === 'scholarships' ? SCHOLARSHIP_ACTIONS : CAREER_ACTIONS;
    return map[this.status()] ?? DEFAULT_ACTION;
  });
  protected readonly ariaLabel = computed(() => `${this.action().label} - ${this.careerName()}`);
  protected readonly resumeQueryParams = computed(() => ({
    idProducto: this.idProducto(),
    idProceso: this.idProceso(),
    estado: this.status(),
    nivel: this.idNivelProducto(),
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
  // Actualización profesional (nivel 3/4) trae una anotación por seminario y todas se
  // reactivan juntas; carrera simple trae una sola. La tarjeta ya manda el set completo.
  protected readonly reactivatesFlow = computed(
    () =>
      this.cardType() === 'careers' &&
      this.status() === 'Dada de baja' &&
      this.idInscripciones().length > 0 &&
      Number.isSafeInteger(this.idProducto()) &&
      (this.idProducto() ?? 0) > 0 &&
      Number.isSafeInteger(this.idProceso()) &&
      (this.idProceso() ?? 0) > 0
  );

  protected reactivate(): void {
    const idsInscripcion = positiveIds(this.idInscripciones());
    if (this.isReactivating() || !idsInscripcion.length) return;
    this.isReactivating.set(true);
    this.inscriptions.reactivate(idsInscripcion).subscribe({
      next: response => {
        this.saveReactivationContext(response);
        this.router.navigate(['/inscripciones'], {
          queryParams: { ...this.resumeQueryParams(), modo: 'reactivar' },
        });
      },
      error: () => this.isReactivating.set(false),
    });
  }

  protected prepareNavigation(): void {
    if (this.status() === 'En proceso') this.saveResumeContext();
    else this.resumeContext.clear();
  }

  private saveResumeContext(): void {
    const idProducto = this.idProducto();
    const idProceso = this.idProceso();
    if (!idProducto || !idProceso) return;

    this.resumeContext.save({
      idProducto,
      idProceso,
      idOfertas: [...this.idOfertas()],
      idInscripciones: [...this.idInscripciones()],
    });
  }

  private saveReactivationContext(response: InscripcionPreEnrollmentResponse): void {
    const idProducto = this.idProducto();
    const idProceso = this.idProceso();
    if (!idProducto || !idProceso) return;

    const responseOffers = positiveIds((response.seminarios ?? []).map(item => item.idOferta));
    const responseInscriptions = positiveIds([
      response.idInscripcion,
      ...(response.seminarios ?? []).map(item => item.idInscripcion),
    ]);

    this.resumeContext.saveReactivation(
      {
        idProducto,
        idProceso,
        idOfertas: responseOffers.length ? responseOffers : [...this.idOfertas()],
        idInscripciones: responseInscriptions,
      },
      response
    );
  }
}

function positiveIds(values: readonly (number | null | undefined)[]): number[] {
  return [
    ...new Set(
      values.filter(
        (value): value is number =>
          typeof value === 'number' && Number.isSafeInteger(value) && value > 0
      )
    ),
  ];
}
