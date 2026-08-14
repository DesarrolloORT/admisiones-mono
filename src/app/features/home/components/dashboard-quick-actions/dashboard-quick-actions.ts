import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import type { EnrollmentPreEnrollmentResponse } from '../../../enrollments/models/enrollment-flow';
import { EnrollmentResumeContextStore } from '../../../enrollments/services/enrollment-resume-context';
import { Enrollments } from '../../../enrollments/services/enrollments';

type CardType = 'enrollments' | 'scholarships';

interface ActionConfig {
  type: 'primary' | 'secondary' | 'text';
  label: string;
  icon?: string;
}

const ENROLLMENT_ACTIONS: Record<string, ActionConfig> = {
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
  private readonly enrollments = inject(Enrollments);
  private readonly router = inject(Router);
  private readonly resumeContext = inject(EnrollmentResumeContextStore);

  readonly status = input.required<string>();
  readonly degreeProgramName = input.required<string>();
  readonly cardType = input<CardType>('enrollments');
  readonly productId = input<number | null>(null);
  readonly admissionProcessId = input<number | null>(null);
  readonly productLevelId = input<number | null>(null);
  readonly enrollmentIds = input<readonly number[]>([]);
  readonly offeringIds = input<readonly number[]>([]);
  private readonly isReactivating = signal(false);

  protected readonly action = computed<ActionConfig>(() => {
    const map = this.cardType() === 'scholarships' ? SCHOLARSHIP_ACTIONS : ENROLLMENT_ACTIONS;
    return map[this.status()] ?? DEFAULT_ACTION;
  });
  protected readonly ariaLabel = computed(
    () => `${this.action().label} - ${this.degreeProgramName()}`
  );
  protected readonly resumeQueryParams = computed(() => ({
    idProducto: this.productId(),
    idProceso: this.admissionProcessId(),
    estado: this.status(),
    nivel: this.productLevelId(),
  }));

  protected readonly resumesFlow = computed(
    () =>
      this.cardType() === 'enrollments' &&
      ['En proceso', 'Pendiente', 'Pago pendiente', 'Confirmada'].includes(this.status()) &&
      Number.isSafeInteger(this.productId()) &&
      Number.isSafeInteger(this.admissionProcessId()) &&
      (this.productId() ?? 0) > 0 &&
      (this.admissionProcessId() ?? 0) > 0
  );
  // Actualización profesional (nivel 3/4) trae una anotación por seminario y todas se
  // reactivan juntas; carrera simple trae una sola. La tarjeta ya manda el set completo.
  protected readonly reactivatesFlow = computed(
    () =>
      this.cardType() === 'enrollments' &&
      this.status() === 'Dada de baja' &&
      this.enrollmentIds().length > 0 &&
      Number.isSafeInteger(this.productId()) &&
      (this.productId() ?? 0) > 0 &&
      Number.isSafeInteger(this.admissionProcessId()) &&
      (this.admissionProcessId() ?? 0) > 0
  );

  protected reactivate(): void {
    const enrollmentIdsToReactivate = positiveIds(this.enrollmentIds());
    if (this.isReactivating() || !enrollmentIdsToReactivate.length) return;
    this.isReactivating.set(true);
    this.enrollments.reactivate(enrollmentIdsToReactivate).subscribe({
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
    const productId = this.productId();
    const admissionProcessId = this.admissionProcessId();
    if (!productId || !admissionProcessId) return;

    this.resumeContext.save({
      productId,
      admissionProcessId,
      offeringIds: [...this.offeringIds()],
      enrollmentIds: [...this.enrollmentIds()],
    });
  }

  private saveReactivationContext(response: EnrollmentPreEnrollmentResponse): void {
    const productId = this.productId();
    const admissionProcessId = this.admissionProcessId();
    if (!productId || !admissionProcessId) return;

    const responseOffers = positiveIds((response.seminars ?? []).map(item => item.offeringId));
    const responseEnrollments = positiveIds([
      response.idEnrollment,
      ...(response.seminars ?? []).map(item => item.idEnrollment),
    ]);

    this.resumeContext.saveReactivation(
      {
        productId,
        admissionProcessId,
        offeringIds: responseOffers.length ? responseOffers : [...this.offeringIds()],
        enrollmentIds: responseEnrollments,
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
