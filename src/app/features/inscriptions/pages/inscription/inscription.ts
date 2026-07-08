import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule, OrtSpinnerModule } from '@desarrolloort/components';

import { ProcessLayout } from '../../../../shared/ui/process-layout/process-layout';
import { AcademicProposalSelection } from '../../../catalogs/services/academic-proposal-selection';
import { InscripcionAcademicStep } from '../../components/inscription-academic-step/inscription-academic-step';
import { InscripcionConfirmationStep } from '../../components/inscription-confirmation-step/inscription-confirmation-step';
import { InscripcionDialog } from '../../components/inscription-dialog/inscription-dialog';
import { InscripcionPersonalStep } from '../../components/inscription-personal-step/inscription-personal-step';
import { InscripcionRegulationReader } from '../../components/inscription-regulation-reader/inscription-regulation-reader';
import { InscripcionReservationStep } from '../../components/inscription-reservation-step/inscription-reservation-step';
import { InscripcionSuccessStep } from '../../components/inscription-success-step/inscription-success-step';
import { InscripcionPaymentFacade } from '../../facades/inscription-payment';
import { InscripcionProcessFacade } from '../../facades/inscription-process';
import { InscripcionProposalFacade } from '../../facades/inscription-proposal';
import { InscripcionSurveyFacade } from '../../facades/inscription-survey';
import { InscripcionSurveyIdentityFacade } from '../../facades/inscription-survey-identity';
import { InscripcionSurveyOptionsFacade } from '../../facades/inscription-survey-options';
import { InscripcionFormsStore } from '../../store/inscription-forms';
import { InscripcionProcessStore } from '../../store/inscription-process';

@Component({
  selector: 'app-inscription',
  imports: [
    InscripcionAcademicStep,
    InscripcionConfirmationStep,
    InscripcionDialog,
    InscripcionPersonalStep,
    InscripcionRegulationReader,
    InscripcionReservationStep,
    InscripcionSuccessStep,
    ProcessLayout,
    OrtButtonModule,
    OrtIconModule,
    OrtSpinnerModule,
    RouterLink,
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
  templateUrl: './inscription.html',
  styleUrl: './inscription.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inscripcion {
  private readonly document = inject(DOCUMENT);
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

      setTimeout(() => this.focusCurrentScreen());
    });
  }

  private focusCurrentScreen(): void {
    const main = this.document.getElementById('main-content');
    main?.focus();
    this.document.defaultView?.scrollTo({ behavior: 'instant', left: 0, top: 0 });
    this.document.defaultView?.setTimeout(() => main?.focus(), 50);
  }
}
