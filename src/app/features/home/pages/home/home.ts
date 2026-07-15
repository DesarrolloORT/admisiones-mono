import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule, OrtStatusIconModule } from '@desarrolloort/components';

import { AuthSessionService } from '../../../auth/services/auth-session';
import { HomeData } from '../../models/home-data';
import { Dashboard } from '../dashboard/dashboard';

interface HomeActionCard {
  id: string;
  title: string;
  description: string;
  icon: string;
  ctaLabel: string;
  imageSrc: string;
  route: string;
}

const HOME_DESCRIPTION = 'Aquí podés gestionar tu inscripción y postulación a becas.';

const ACTION_CARDS: HomeActionCard[] = [
  {
    id: 'career',
    title: 'Inscripción a carrera',
    description: 'Iniciá tu inscripción y reservá tu lugar para el próximo inicio.',
    icon: 'school',
    ctaLabel: 'Comenzar inscripción',
    imageSrc: 'assets/home/inscripcion-card.png',
    route: '/inscripciones',
  },
  {
    id: 'scholarship',
    title: 'Postulación a becas',
    description: 'Podés postularte a las oportunidades de beca disponibles.',
    icon: 'workspace_premium',
    ctaLabel: 'Postularme a beca',
    imageSrc: 'assets/home/becas-card.png',
    route: '/becas',
  },
];

@Component({
  selector: 'app-home',
  imports: [Dashboard, OrtButtonModule, OrtIconModule, RouterLink, OrtStatusIconModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  private readonly authSession = inject(AuthSessionService);

  readonly homeData = input.required<HomeData | null>();

  protected readonly hasActivity = computed(() => {
    const data = this.homeData();
    return !!data && (data.inscripciones.length > 0 || data.becas.length > 0);
  });
  protected readonly description = HOME_DESCRIPTION;
  protected readonly actionCards = ACTION_CARDS;

  protected readonly greeting = computed(() => {
    const userName = this.authSession.session()?.primerNombre?.trim();

    return userName ? `¡Hola ${userName}!` : '¡Hola!';
  });
}
