import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
  ViewEncapsulation,
} from '@angular/core';
import Swiper from 'swiper';
import { Navigation, Pagination } from 'swiper/modules';

import { DashboardCard } from '../../components/dashboard-card/dashboard-card';
import { MiInscripcion } from '../../models/mi-inscripcion';
import { DashboardService } from '../../services/dashboard';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  imports: [DashboardCard],
})
export class Dashboard implements OnInit, AfterViewInit {
  private readonly service = inject(DashboardService);

  protected readonly inscripciones = signal<MiInscripcion[]>([]);

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

