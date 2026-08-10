import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  OnDestroy,
  signal,
  ViewEncapsulation,
} from '@angular/core';
import { Router } from '@angular/router';
import { OrtAlertModule, OrtButton, OrtDialog, OrtIconModule } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import Swiper from 'swiper';
import { Navigation, Pagination } from 'swiper/modules';

import { AuthSessionService } from '../../../auth/services/auth-session';
import { InscriptionResumeContextStore } from '../../../inscriptions/services/inscription-resume-context';
import { DashboardActionCard } from '../../components/dashboard-action-card/dashboard-action-card';
import { DashboardCareersSection } from '../../components/dashboard-careers-section/dashboard-careers-section';
import { DashboardScholarshipsSection } from '../../components/dashboard-scholarships-section/dashboard-scholarships-section';
import { ReviewScholarshipResult } from '../../components/review-scholarship-result/review-scholarship-result';
import { MiBeca } from '../../models/mi-beca';
import {
  buildPendingPaymentSummary,
  MiInscripcion,
  PENDING_PAYMENT_STATUS,
} from '../../models/mi-inscripcion';

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
    OrtDialog,
    OrtButton,
    OrtIconModule,
    ReviewScholarshipResult,
  ],
})
export class Dashboard implements AfterViewInit, OnDestroy {
  private readonly authSession = inject(AuthSessionService);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly router = inject(Router);
  private readonly resumeContext = inject(InscriptionResumeContextStore);
  private readonly swipers: Swiper[] = [];

  readonly inscripciones = input.required<MiInscripcion[]>();
  readonly becas = input.required<MiBeca[]>();

  protected readonly greeting = computed(() => {
    const name = this.authSession.session()?.primerNombre?.trim();
    return name ? `¡Hola ${name}!` : '¡Hola!';
  });
  protected readonly hasPendingPayment = computed(() =>
    this.inscripciones().some(i => i.estado === PENDING_PAYMENT_STATUS)
  );
  protected readonly pendingPaymentSummary = computed(() =>
    buildPendingPaymentSummary(this.inscripciones())
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

  private readonly breakpointService = inject(BreakpointService);

  readonly hideStatusIcon = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall;
  });

  readonly isDialogOpen = signal(false);
  readonly showScholarshipResult = signal(false);

  openDialog() {
    this.isDialogOpen.set(true);
  }

  closeDialog() {
    this.isDialogOpen.set(false);
  }

  showScholarshipReview() {
    this.showScholarshipResult.set(true);
  }

  closeReview() {
    this.showScholarshipResult.set(false);
  }

  protected navigateToPendingPayment(): void {
    const target = this.pendingPaymentSummary().target;
    if (!target) return;

    this.resumeContext.clear();
    this.router.navigate(['/inscripciones'], { queryParams: target });
  }
}
