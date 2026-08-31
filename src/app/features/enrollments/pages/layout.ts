import { DOCUMENT } from '@angular/common';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  Injector,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtIconModule,
  OrtSpinnerModule,
  OrtStatusIconModule,
} from '@desarrolloort/components';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { EnrollmentPaymentFacade } from '../facades/enrollment-payment';
import { EnrollmentProcessFacade } from '../facades/enrollment-process';
import { EnrollmentProposalFacade } from '../facades/enrollment-proposal';
import { EnrollmentSurveyFacade } from '../facades/enrollment-survey';
import { EnrollmentSurveyIdentityFacade } from '../facades/enrollment-survey-identity';
import { EnrollmentSurveyOptionsFacade } from '../facades/enrollment-survey-options';
import { createEnrollmentFormsState, ENROLLMENT_FORMS } from '../models/enrollment-flow-forms';
import {
  createEnrollmentProcessState,
  ENROLLMENT_PROCESS_STATE,
} from '../models/enrollment-process';
import { EnrollmentAcademicStep } from './steps/enrollment-academic-step/enrollment-academic-step';
import { EnrollmentConfirmationStep } from './steps/enrollment-confirmation-step/enrollment-confirmation-step';
import { EnrollmentPersonalStep } from './steps/enrollment-personal-step/enrollment-personal-step';
import { EnrollmentRegulationReader } from './steps/enrollment-regulation-reader/enrollment-regulation-reader';
import { EnrollmentReservationStep } from './steps/enrollment-reservation-step/enrollment-reservation-step';
import { EnrollmentSuccessStep } from './steps/enrollment-success-step/enrollment-success-step';

@Component({
  selector: 'app-enrollment',
  imports: [
    EnrollmentAcademicStep,
    EnrollmentConfirmationStep,
    EnrollmentPersonalStep,
    EnrollmentRegulationReader,
    EnrollmentReservationStep,
    EnrollmentSuccessStep,
    ProcessLayout,
    OrtButtonModule,
    OrtIconModule,
    OrtSpinnerModule,
    RouterLink,
    OrtStatusIconModule,
  ],
  providers: [
    AcademicProposalSelection,
    { provide: ENROLLMENT_FORMS, useFactory: createEnrollmentFormsState },
    { provide: ENROLLMENT_PROCESS_STATE, useFactory: createEnrollmentProcessState },
    EnrollmentProposalFacade,
    EnrollmentSurveyOptionsFacade,
    EnrollmentSurveyIdentityFacade,
    EnrollmentSurveyFacade,
    EnrollmentPaymentFacade,
    EnrollmentProcessFacade,
  ],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Layout {
  private readonly authSession = inject(AuthSessionService);
  private readonly document = inject(DOCUMENT);
  private readonly injector = inject(Injector);
  protected readonly process = inject(EnrollmentProcessFacade);
  protected readonly survey = inject(EnrollmentSurveyFacade);
  protected readonly payment = inject(EnrollmentPaymentFacade);

  constructor() {
    effect(() => {
      this.process.currentStep();
      this.survey.activeSection();
      this.survey.readerOpen();
      this.payment.outcome();

      afterNextRender(() => this.focusCurrentScreen(), { injector: this.injector });
    });
  }

  protected logout(): void {
    this.authSession.logout();
  }

  private focusCurrentScreen(): void {
    const main = this.document.getElementById('main-content');
    main?.focus();
    this.document.defaultView?.scrollTo({ behavior: 'instant', left: 0, top: 0 });
  }
}
