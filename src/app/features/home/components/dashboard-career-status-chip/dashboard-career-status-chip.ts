import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtChipModule } from '@desarrolloort/components';

type ChipVariant = 'warning' | 'information' | 'success' | 'error';

const VARIANT_MAP: Record<string, ChipVariant> = {
  'En proceso': 'warning',
  'Pendiente': 'warning',
  'A la espera': 'information',
  'Confirmada': 'success',
  'Cancelada': 'error',
};

const DEFAULT_VARIANT: ChipVariant = 'information';

@Component({
  selector: 'app-dashboard-career-status-chip',
  imports: [OrtChipModule],
  templateUrl: './dashboard-career-status-chip.html',
  styleUrl: './dashboard-career-status-chip.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCareerStatusChip {
  readonly status = input.required<string>();

  protected readonly variant = computed<ChipVariant>(() => VARIANT_MAP[this.status()] ?? DEFAULT_VARIANT);
}

