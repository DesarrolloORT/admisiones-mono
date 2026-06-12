import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtIconButtonComponent, OrtIconModule } from '@desarrolloort/components';

type SectionVariant = 'careers' | 'scholarships';

const VARIANT_CONFIG: Record<SectionVariant, { title: string; ariaLabel: string }> = {
  careers: { title: 'Mis carreras', ariaLabel: 'Inscribirme a carrera' },
  scholarships: { title: 'Mis becas', ariaLabel: 'Postularme a beca' },
};

@Component({
  selector: 'app-dashboard-section-header',
  imports: [OrtIconButtonComponent, OrtIconModule],
  templateUrl: './dashboard-section-header.html',
  styleUrl: './dashboard-section-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardSectionHeader {
  readonly variant = input.required<SectionVariant>();

  protected readonly config = computed(() => VARIANT_CONFIG[this.variant()]);
}
