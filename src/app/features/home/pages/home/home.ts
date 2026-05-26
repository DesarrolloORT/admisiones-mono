import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import { AuthSessionService } from '../../../auth/services/auth-session';
import { HomeActionCard, HomeDashboard } from '../../models/home-dashboard';

const HOME_DESCRIPTION = 'Aquí podés gestionar tu inscripción y postulación a becas.';

const ACTION_CARDS: HomeActionCard[] = [
  {
    id: 'career',
    title: 'Inscripción a carrera',
    description: 'Iniciá tu inscripción y reservá tu lugar para el próximo inicio.',
    icon: 'school',
    ctaLabel: 'Comenzar inscripción',
    imageSrc: 'assets/home/inscripcion-card.png',
    disabledReason: 'Disponible próximamente.',
  },
  {
    id: 'scholarship',
    title: 'Postulación a becas',
    description: 'Podés postularte a las oportunidades de beca disponibles.',
    icon: 'workspace_premium',
    ctaLabel: 'Postularme a beca',
    imageSrc: 'assets/home/becas-card.png',
    disabledReason: 'Disponible próximamente.',
  },
];

@Component({
  selector: 'app-home',
  imports: [OrtButtonModule, OrtIconModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  private readonly authSession = inject(AuthSessionService);

  protected readonly dashboard = computed<HomeDashboard>(() => {
    const session = this.authSession.session();

    return {
      userName: session?.primerNombre?.trim() || '',
      description: HOME_DESCRIPTION,
      actionCards: ACTION_CARDS,
    };
  });

  protected readonly greeting = computed(() => {
    const userName = this.dashboard().userName;

    return userName ? `¡Hola ${userName}!` : '¡Hola!';
  });
}
