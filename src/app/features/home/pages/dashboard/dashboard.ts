import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
  ViewEncapsulation,
} from '@angular/core';
import { OrtAlertModule, OrtIconButtonComponent, OrtIconModule } from '@desarrolloort/components';
import Swiper from 'swiper';
import { Navigation, Pagination } from 'swiper/modules';

import { AuthSessionService } from '../../../auth/services/auth-session';
import { DashboardActionCard } from "../../components/dashboard-action-card/dashboard-action-card";
import { DashboardCard } from '../../components/dashboard-card/dashboard-card';
import { DashboardSectionHeader } from '../../components/dashboard-section-header/dashboard-section-header';
import { MiInscripcion } from '../../models/mi-inscripcion';
import { DashboardService } from '../../services/dashboard';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  imports: [DashboardCard, OrtIconButtonComponent, OrtIconModule, DashboardActionCard, OrtAlertModule, DashboardSectionHeader],
})
export class Dashboard implements OnInit, AfterViewInit {
  private readonly service = inject(DashboardService);
  private readonly authSession = inject(AuthSessionService);

  protected readonly inscripciones = signal<MiInscripcion[]>([]);
  protected readonly greeting = computed(() => {
    const name = this.authSession.session()?.primerNombre?.trim();
    return name ? `¡Hola ${name}!` : '¡Hola!';
  });
  protected readonly hasPendingPayment = computed(() =>
    this.inscripciones().some(i => i.estado === 'Pago pendiente')
  );

  ngOnInit(): void {
    this.service.getMisInscripciones().subscribe(data => {
      this.inscripciones.set(data);
      console.log('Mis inscripciones:', this.inscripciones());
    });
  }

  ngAfterViewInit(): void {
    new Swiper('.swiper', {
      spaceBetween: 24,
      direction: 'horizontal',
      loop: false,
      slidesPerView: 1,
      modules: [Navigation, Pagination],
      pagination: {
        el: '.swiper-pagination',
        clickable: true
      },
      navigation: {
        nextEl: '.swiper-button-next',
        prevEl: '.swiper-button-prev',
      },
      breakpoints: {
        768: {
          slidesPerView: 2,
        },
        1200: {
          slidesPerView: 3,
        },
      },
    });
  }
}

