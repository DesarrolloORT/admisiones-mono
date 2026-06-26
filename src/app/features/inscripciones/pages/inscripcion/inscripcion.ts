import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule, OrtSpinnerModule } from '@desarrolloort/components';

import { ProcessLayout } from '../../../../shared/ui/process-layout/process-layout';
import { AcademicProposalSelection } from '../../../catalogs/services/academic-proposal-selection';
import { InscripcionAcademicStep } from '../../components/inscripcion-academic-step/inscripcion-academic-step';
import { InscripcionConfirmationStep } from '../../components/inscripcion-confirmation-step/inscripcion-confirmation-step';
import { InscripcionDialog } from '../../components/inscripcion-dialog/inscripcion-dialog';
import { InscripcionPersonalStep } from '../../components/inscripcion-personal-step/inscripcion-personal-step';
import { InscripcionRegulationReader } from '../../components/inscripcion-regulation-reader/inscripcion-regulation-reader';
import { InscripcionReservationStep } from '../../components/inscripcion-reservation-step/inscripcion-reservation-step';
import { InscripcionSuccessStep } from '../../components/inscripcion-success-step/inscripcion-success-step';
import { InscripcionPaymentFacade } from '../../facades/inscripcion-payment';
import { InscripcionProcessFacade } from '../../facades/inscripcion-process';
import { InscripcionProposalFacade } from '../../facades/inscripcion-proposal';
import { InscripcionSurveyFacade } from '../../facades/inscripcion-survey';
import { InscripcionFormsStore } from '../../store/inscripcion-forms';
import { InscripcionProcessStore } from '../../store/inscripcion-process';

@Component({
  selector: 'app-inscripcion',
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
    InscripcionSurveyFacade,
    InscripcionPaymentFacade,
    InscripcionProcessFacade,
  ],
  templateUrl: './inscripcion.html',
  styleUrl: './inscripcion.scss',
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
