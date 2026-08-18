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
import { Router } from '@angular/router';
import { OrtAlertModule } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import Swiper from 'swiper';
import { Navigation, Pagination } from 'swiper/modules';

import { AuthSessionService } from '../../../auth/services/auth-session';
import { EnrollmentResumeContextStore } from '../../../enrollments/services/enrollment-resume-context';
import { DashboardActionCard } from '../../components/dashboard-action-card/dashboard-action-card';
import { DashboardEnrollmentsSection } from '../../components/dashboard-enrollments-section/dashboard-enrollments-section';
import { DashboardScholarshipsSection } from '../../components/dashboard-scholarships-section/dashboard-scholarships-section';
import {
  buildPendingPaymentSummary,
  EnrollmentSummary,
  PENDING_PAYMENT_STATUS,
} from '../../models/enrollment-summary';
import { ScholarshipSummary } from '../../models/scholarship-summary';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  imports: [
    DashboardActionCard,
    DashboardEnrollmentsSection,
    OrtAlertModule,
    DashboardScholarshipsSection,
  ],
})
export class Dashboard implements AfterViewInit, OnDestroy {
  private readonly authSession = inject(AuthSessionService);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly router = inject(Router);
  private readonly resumeContext = inject(EnrollmentResumeContextStore);
  private readonly swipers: Swiper[] = [];
  private readonly breakpointService = inject(BreakpointService);

  readonly enrollments = input.required<EnrollmentSummary[]>();
  readonly scholarships = input.required<ScholarshipSummary[]>();

  protected readonly greeting = computed(() => {
    const name = this.authSession.session()?.firstName?.trim();
    return name ? `¡Hola ${name}!` : '¡Hola!';
  });
  protected readonly hasPendingPayment = computed(() =>
    this.enrollments().some(enrollment => enrollment.status === PENDING_PAYMENT_STATUS)
  );
  protected readonly pendingPaymentSummary = computed(() =>
    buildPendingPaymentSummary(this.enrollments())
  );
  protected readonly singleRow = computed(
    () => this.enrollments().length === 1 && this.scholarships().length === 1
  );
  readonly hideStatusIcon = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();
    return breakpoint.isXSmall || breakpoint.isSmall;
  });

  ngAfterViewInit(): void {
    this.elementRef.nativeElement.querySelectorAll<HTMLElement>('.swiper').forEach(element => {
      if (!element.querySelector('.swiper-slide')) return;

      this.swipers.push(
        new Swiper(element, {
          spaceBetween: 24,
          direction: 'horizontal',
          loop: false,
          slidesPerView: 1,
          modules: [Navigation, Pagination],
          pagination: { el: '.swiper-pagination', clickable: true },
          navigation: { nextEl: '.swiper-button-next', prevEl: '.swiper-button-prev' },
          breakpoints: {
            768: { slidesPerView: 2 },
            1200: { slidesPerView: 3 },
          },
        })
      );
    });
  }

  ngOnDestroy(): void {
    this.swipers.forEach(swiper => swiper.destroy());
  }

  protected navigateToPendingPayment(): void {
    const target = this.pendingPaymentSummary().target;
    if (!target) return;

    this.resumeContext.clear();
    this.router.navigate(['/inscripciones'], { queryParams: target });
  }
}
