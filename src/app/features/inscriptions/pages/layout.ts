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
  OrtDialog,
  OrtIconModule,
  OrtSpinnerModule,
  OrtStatusIconModule,
} from '@desarrolloort/components';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { InscripcionPaymentFacade } from '../facades/inscription-payment';
import { InscripcionProcessFacade } from '../facades/inscription-process';
import { InscripcionProposalFacade } from '../facades/inscription-proposal';
import { InscripcionSurveyFacade } from '../facades/inscription-survey';
import { InscripcionSurveyIdentityFacade } from '../facades/inscription-survey-identity';
import { InscripcionSurveyOptionsFacade } from '../facades/inscription-survey-options';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionAcademicStep } from './steps/inscription-academic-step/inscription-academic-step';
import { InscripcionConfirmationStep } from './steps/inscription-confirmation-step/inscription-confirmation-step';
import { InscripcionPersonalStep } from './steps/inscription-personal-step/inscription-personal-step';
import { InscripcionRegulationReader } from './steps/inscription-regulation-reader/inscription-regulation-reader';
import { InscripcionReservationStep } from './steps/inscription-reservation-step/inscription-reservation-step';
import { InscripcionSuccessStep } from './steps/inscription-success-step/inscription-success-step';

@Component({
  selector: 'app-inscription',
  imports: [
    InscripcionAcademicStep,
    InscripcionConfirmationStep,
    InscripcionPersonalStep,
    InscripcionRegulationReader,
    InscripcionReservationStep,
    InscripcionSuccessStep,
    ProcessLayout,
    OrtButtonModule,
    OrtDialog,
    OrtIconModule,
    OrtSpinnerModule,
    RouterLink,
    OrtStatusIconModule,
  ],
  providers: [
    AcademicProposalSelection,
    InscripcionFormsStore,
    InscripcionProcessStore,
    InscripcionProposalFacade,
    InscripcionSurveyOptionsFacade,
    InscripcionSurveyIdentityFacade,
    InscripcionSurveyFacade,
    InscripcionPaymentFacade,
    InscripcionProcessFacade,
  ],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Layout {
  private readonly document = inject(DOCUMENT);
  private readonly injector = inject(Injector);
  protected readonly process = inject(InscripcionProcessFacade);
  protected readonly survey = inject(InscripcionSurveyFacade);
  protected readonly payment = inject(InscripcionPaymentFacade);

  constructor() {
    effect(() => {
      this.process.currentStep();
      this.survey.activeSection();
      this.survey.readerOpen();
      this.payment.outcome();
      if (this.payment.view() === 'confirming') return;

      afterNextRender(() => this.focusCurrentScreen(), { injector: this.injector });
    });
  }

  private focusCurrentScreen(): void {
    const main = this.document.getElementById('main-content');
    main?.focus();
    this.document.defaultView?.scrollTo({ behavior: 'instant', left: 0, top: 0 });
  }
}
