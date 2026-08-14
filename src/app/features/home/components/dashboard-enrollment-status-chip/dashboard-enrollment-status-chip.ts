import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtChipModule } from '@desarrolloort/components';

type ChipVariant = 'warning' | 'information' | 'success' | 'error';

const VARIANT_MAP: Record<string, ChipVariant> = {
  'En proceso': 'warning',
  'Pago pendiente': 'warning',
  'A la espera': 'information',
  Confirmada: 'success',
  'Dada de baja': 'error',
  Aceptada: 'success',
};

const DEFAULT_VARIANT: ChipVariant = 'information';

@Component({
  selector: 'app-dashboard-enrollment-status-chip',
  imports: [OrtChipModule],
  templateUrl: './dashboard-enrollment-status-chip.html',
  styleUrl: './dashboard-enrollment-status-chip.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardEnrollmentStatusChip {
  readonly status = input.required<string>();

  protected readonly variant = computed<ChipVariant>(
    () => VARIANT_MAP[this.status()] ?? DEFAULT_VARIANT
  );
}
