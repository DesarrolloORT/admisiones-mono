import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

export type ActionCardVariant = 'career' | 'scholarship';

interface ActionCardConfig {
  icon: string;
  title: string;
  description: string;
  buttonText: string;
}

const VARIANT_CONFIG: Record<ActionCardVariant, ActionCardConfig> = {
  career: {
    icon: 'school',
    title: 'Inscripción a carrera',
    description: 'Iniciá tu inscripción y reservá tu lugar.',
    buttonText: 'Comenzar inscripción',
  },
  scholarship: {
    icon: 'workspace_premium',
    title: 'Postulación a becas',
    description: 'Seleccioná la beca más adecuada a tu perfil.',
    buttonText: 'Postularme a beca',
  },
};

@Component({
  selector: 'app-dashboard-action-card',
  imports: [OrtIconModule, OrtButtonModule],
  templateUrl: './dashboard-action-card.html',
  styleUrl: './dashboard-action-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardActionCard {
  readonly variant = input.required<ActionCardVariant>();

  protected readonly config = computed<ActionCardConfig>(() => VARIANT_CONFIG[this.variant()]);
}
