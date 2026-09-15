import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule, OrtStatusIconModule } from '@desarrolloort/components';

import { AuthSessionService } from '../../../auth/services/auth-session';
import { HomeData, HomeResolved, isHomeLoadFailure } from '../../models/home-data';
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
    id: 'enrollment',
    title: 'Inscripción a carrera',
    description: 'Iniciá tu inscripción y reservá tu lugar para el próximo inicio.',
    icon: 'school',
    ctaLabel: 'Comenzar inscripción',
    imageSrc: 'assets/home/enrollment-card.png',
    route: '/inscripciones',
  },
  {
    id: 'scholarship',
    title: 'Postulación a becas',
    description: 'Podés postularte a las oportunidades de beca disponibles.',
    icon: 'workspace_premium',
    ctaLabel: 'Postularme a beca',
    imageSrc: 'assets/home/scholarships-card.png',
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

  readonly homeData = input.required<HomeResolved>();

  protected readonly loadError = computed(() => {
    const resolved = this.homeData();
    return isHomeLoadFailure(resolved) ? resolved.loadError : null;
  });

  protected readonly data = computed<HomeData | null>(() => {
    const resolved = this.homeData();
    return isHomeLoadFailure(resolved) ? null : resolved;
  });

  protected readonly hasActivity = computed(() => {
    const data = this.data();
    return !!data && (data.enrollments.length > 0 || data.scholarships.length > 0);
  });
  protected readonly description = HOME_DESCRIPTION;
  protected readonly actionCards = ACTION_CARDS;

  protected readonly greeting = computed(() => {
    const userName = this.authSession.session()?.firstName?.trim();

    return userName ? `¡Hola ${userName}!` : '¡Hola!';
  });
}
