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
import { DashboardActionCard } from '../../components/dashboard-action-card/dashboard-action-card';
import { DashboardCard } from '../../components/dashboard-card/dashboard-card';
import { DashboardSectionHeader } from '../../components/dashboard-section-header/dashboard-section-header';
import { MiBeca } from '../../models/mi-beca';
import { MiInscripcion } from '../../models/mi-inscripcion';
import { DashboardService } from '../../services/dashboard';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  imports: [
    DashboardCard,
    OrtIconButtonComponent,
    OrtIconModule,
    DashboardActionCard,
    OrtAlertModule,
    DashboardSectionHeader,
  ],
})
export class Dashboard implements OnInit, AfterViewInit {
  private readonly service = inject(DashboardService);
  private readonly authSession = inject(AuthSessionService);

  protected readonly inscripciones = signal<MiInscripcion[]>([]);
  protected readonly becas = signal<MiBeca[]>([
    {
      id: 1,
      nombreBeca: 'Fondo de Capacitación Laboral',
      nombreCarrera: 'Analista programador',
      estado: 'En proceso',
      cierrePostulacion: 'Lunes 15/07/2026',
      fechaPrueba: 'Jueves 22/07/2026',
      resultadoPrueba: '',
      beneficio: '',
      fechaResultados: '',
    },
    {
      id: 2,
      nombreBeca: 'Fondo de Becas Concursables',
      nombreCarrera: 'Contador público',
      estado: 'Consulta',
      cierrePostulacion: '',
      fechaPrueba: '',
      resultadoPrueba: '32/1600 pts.',
      beneficio: 'Sin otorgamiento',
      fechaResultados: '',
    },
    {
      id: 3,
      nombreBeca: 'Fondo de Excelencia Académica',
      nombreCarrera: 'Licenciatura en Diseño Gráfico',
      estado: 'Aceptada',
      cierrePostulacion: '',
      fechaPrueba: '',
      resultadoPrueba: '1500/1600 pts.',
      beneficio: 'Beca del 25%',
      fechaResultados: '',
    },
    {
      id: 4,
      nombreBeca: 'Fondo de Becas Concursables',
      nombreCarrera: 'Contador público',
      estado: 'Estudio',
      cierrePostulacion: '',
      fechaPrueba: 'Viernes 13/06/2026',
      resultadoPrueba: '',
      beneficio: '',
      fechaResultados: 'Lunes 24/07/2026',
    },
  ]);
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

  ngOnInit(): void {
    this.service.getMisInscripciones().subscribe(data => {
      this.inscripciones.set(data.slice());
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
    });
  }
}

