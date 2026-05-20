import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import { Auth } from '../../../auth/services/auth';
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
  },
  {
    id: 'scholarship',
    title: 'Postulación a becas',
    description: 'Podés postularte a las oportunidades de beca disponibles.',
    icon: 'workspace_premium',
    ctaLabel: 'Postularme a beca',
    imageSrc: 'assets/home/becas-card.png',
  },
];

@Component({
  selector: 'app-home',
  imports: [OrtButtonModule, OrtIconModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  private readonly auth = inject(Auth);

  protected readonly profileMenuOpen = signal(false);

  protected readonly dashboard = computed<HomeDashboard>(() => {
    const session = this.auth.session();

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

  protected toggleProfileMenu(): void {
    this.profileMenuOpen.update(isOpen => !isOpen);
  }

  protected closeProfileMenu(): void {
    this.profileMenuOpen.set(false);
  }

  protected logout(): void {
    this.auth.logout();
    this.closeProfileMenu();
  }
}

