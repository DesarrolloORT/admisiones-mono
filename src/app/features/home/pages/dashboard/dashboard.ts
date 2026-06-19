import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  OnDestroy,
  ViewEncapsulation,
} from '@angular/core';
import { OrtAlertModule } from '@desarrolloort/components';
import Swiper from 'swiper';
import { Navigation, Pagination } from 'swiper/modules';

import { AuthSessionService } from '../../../auth/services/auth-session';
import { DashboardActionCard } from '../../components/dashboard-action-card/dashboard-action-card';
import { DashboardCareersSection } from '../../components/dashboard-careers-section/dashboard-careers-section';
import { DashboardScholarshipsSection } from '../../components/dashboard-scholarships-section/dashboard-scholarships-section';
import { MiBeca } from '../../models/mi-beca';
import { MiInscripcion } from '../../models/mi-inscripcion';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  imports: [
    DashboardActionCard,
    DashboardCareersSection,
    OrtAlertModule,
    DashboardScholarshipsSection,
  ],
})
export class Dashboard implements AfterViewInit, OnDestroy {
  private readonly authSession = inject(AuthSessionService);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly swipers: Swiper[] = [];

  readonly inscripciones = input.required<MiInscripcion[]>();
  readonly becas = input.required<MiBeca[]>();

  protected readonly greeting = computed(() => {
    const name = this.authSession.session()?.primerNombre?.trim();
    return name ? `¡Hola ${name}!` : '¡Hola!';
  });
  protected readonly hasPendingPayment = computed(() =>
    this.inscripciones().some(i => i.estado === 'Pago pendiente')
  );

  protected readonly singleRow = computed(
    () => this.inscripciones().length === 1 && this.becas().length === 1
  );

  ngAfterViewInit(): void {
    this.elementRef.nativeElement.querySelectorAll<HTMLElement>('.swiper').forEach(element => {
      if (!element.querySelector('.swiper-slide')) {
        return;
      }

      this.swipers.push(
        new Swiper(element, {
          spaceBetween: 24,
          direction: 'horizontal',
          loop: false,
          slidesPerView: 1,
          modules: [Navigation, Pagination],
          pagination: {
            el: '.swiper-pagination',
            clickable: true,
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
        })
      );
    });
  }

  ngOnDestroy(): void {
    this.swipers.forEach(swiper => swiper.destroy());
  }
}
