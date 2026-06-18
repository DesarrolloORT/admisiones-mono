import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

export type ActionCardVariant = 'career' | 'scholarship';

interface ActionCardConfig {
  icon: string;
  title: string;
  description: string;
  route: string;
}

const VARIANT_CONFIG: Record<ActionCardVariant, ActionCardConfig> = {
  career: {
    icon: 'school',
    title: 'Inscripción a carrera',
    description: 'Iniciá tu inscripción y reservá tu lugar.',
    route: '/inscripciones',
  },
  scholarship: {
    icon: 'workspace_premium',
    title: 'Postulación a becas',
    description: 'Seleccioná la beca más adecuada a tu perfil.',
    route: '/becas',
  },
};

@Component({
  selector: 'app-dashboard-action-card',
  imports: [OrtIconModule, OrtButtonModule, RouterLink],
  templateUrl: './dashboard-action-card.html',
  styleUrl: './dashboard-action-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardActionCard {
  readonly variant = input.required<ActionCardVariant>();

  protected readonly config = computed<ActionCardConfig>(() => VARIANT_CONFIG[this.variant()]);
}
