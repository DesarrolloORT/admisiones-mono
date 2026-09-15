import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { HomeHeader } from 'src/app/shared/ui/home-header/home-header';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { type ScholarshipVariant } from '../../models/scholarship-personal-forms';
import { ScholarshipAcademicStep } from './steps/scholarship-academic-step/scholarship-academic-step';
import { ScholarshipConfirmationStep } from './steps/scholarship-confirmation-step/scholarship-confirmation-step';
import { ScholarshipOnboardingStep } from './steps/scholarship-onboarding-step/scholarship-onboarding-step';
import { ScholarshipPersonalStep } from './steps/scholarship-personal-step/scholarship-personal-step';
import { ScholarshipSuccessStep } from './steps/scholarship-success-step/scholarship-success-step';

/** Beca que se está postulando, tal como la declara la ruta. */
export type ScholarshipKind = 'fbr' | 'fbc' | 'fcl' | 'fexa';

/**
 * Página del proceso de postulación a becas. Es **una sola** para las cuatro
 * becas: lo único que cambia entre ellas es la variante, que llega por `data`
 * de la ruta y decide qué secciones y validadores aplican.
 *
 * `fexa` es la excepción: su variante depende de si la persona declara ingresos
 * o no, así que se deriva del propio formulario en vez de la ruta.
 */
@Component({
  selector: 'app-scholarship-process',
  imports: [
    ProcessLayout,
    HomeHeader,
    ScholarshipOnboardingStep,
    ScholarshipAcademicStep,
    ScholarshipPersonalStep,
    ScholarshipConfirmationStep,
    ScholarshipSuccessStep,
  ],
  providers: [ScholarshipProcessFacade],
  templateUrl: './scholarship-process.html',
  styleUrl: './scholarship-process.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipProcess {
  private readonly document = inject(DOCUMENT);
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly process = inject(ScholarshipProcessFacade);

  private readonly kind = inject(ActivatedRoute).snapshot.data['kind'] as ScholarshipKind;

  protected readonly onboardingCompleted = signal(false);
  protected readonly success = signal(false);

  /**
   * `fexa` se divide en dos variantes según el modo de postulación elegido en
   * el paso 1; el resto de las becas mapea uno a uno con la ruta.
   */
  protected readonly variant = computed<ScholarshipVariant>(() => {
    if (this.kind !== 'fexa') {
      return this.kind;
    }

    return this.process.applicationMode() === 'sin declaracion' ? 'fexaSin' : 'fexaCon';
  });

  protected readonly showBack = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall;
  });

  constructor() {
    effect(() => {
      this.onboardingCompleted();
      this.process.currentStep();
      this.success();

      setTimeout(() => this.focusCurrentScreen());
    });
  }

  protected startApplication(): void {
    this.onboardingCompleted.set(true);
  }

  protected goHome(): void {
    void this.router.navigate(['/inicio']);
  }

  protected logout(): void {
    this.authSession.logout();
  }

  protected showSuccess(): void {
    this.success.set(true);
  }

  private focusCurrentScreen(): void {
    const main = this.document.getElementById('main-content');
    main?.focus();
    this.document.defaultView?.scrollTo({ behavior: 'instant', left: 0, top: 0 });
    this.document.defaultView?.setTimeout(() => main?.focus(), 50);
  }
}
