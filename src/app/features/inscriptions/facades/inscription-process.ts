import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs/operators';

import {
  deriveInitialInscripcionState,
  type InscripcionEntryContext,
  type InscripcionEntryResolved,
  type InscripcionInitialState,
  type InscripcionInitialSurveyResolved,
  type InscripcionPaymentInit,
} from '../models/inscription-entry';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProposalFacade } from './inscription-proposal';
import { InscripcionSurveyFacade } from './inscription-survey';

export class InscripcionProcessFacade {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly process = inject(InscripcionProcessStore);

  public readonly proposal = inject(InscripcionProposalFacade);
  public readonly survey = inject(InscripcionSurveyFacade);
  public readonly payment = inject(InscripcionPaymentFacade);

  public readonly currentStep = this.process.flow.currentStep;
  public readonly stepItems = computed(() => [...this.process.flow.stepItems()]);
  public readonly stepNumber = computed(() => this.process.flow.currentIndex() + 1);
  public readonly stepLabel = computed(() => {
    const step = this.stepItems()[this.process.flow.currentIndex()];
    return `Paso ${this.stepNumber()} de ${this.stepItems().length} - ${step.title}`;
  });
  public readonly backLabel = computed(() => {
    if (this.currentStep() === 'pago') return 'Volver al paso 2';
    if (this.currentStep() !== 'encuesta') return 'Volver al paso anterior';
    if (this.survey.readerOpen()) return 'Volver a Reglamento estudiantil';

    const sections = this.survey.visibleSections();
    return sections.indexOf(this.survey.activeSection()) > 0
      ? 'Volver a la sección anterior'
      : 'Volver al paso 1';
  });
  public readonly exitConfirmationOpen = signal(false);
  public readonly surveySaveError = signal<string | null>(null);
  public readonly savingSurvey = signal(false);
  public readonly catalogError = computed(
    () => this.proposal.catalogError() ?? this.survey.catalogError()
  );
  public readonly showStepper = computed(
    () =>
      !this.survey.loadingSurveyState() &&
      !this.survey.surveyLoadError() &&
      !this.survey.readerOpen() &&
      !this.payment.outcome() &&
      this.payment.view() !== 'processing'
  );
  public readonly canGoBack = computed(() => {
    if (this.payment.outcome() || this.payment.view() === 'processing') return false;
    if (this.currentStep() === 'propuesta') return false;
    if (this.currentStep() === 'pago') return this.payment.view() === 'editing';
    if (this.survey.readerOpen()) return true;
    return (
      this.survey.visibleSections().indexOf(this.survey.activeSection()) > 0 ||
      this.process.flow.canGoBack()
    );
  });

  private entryContext: InscripcionEntryContext;

  constructor() {
    this.entryContext = {
      entry: this.route.snapshot.data['entry'] as InscripcionEntryResolved,
      survey: this.route.snapshot.data['initialSurvey'] as InscripcionInitialSurveyResolved,
    };
    this.applyInitialState(deriveInitialInscripcionState(this.entryContext));
  }

  public continue(): void {
    switch (this.currentStep()) {
      case 'propuesta':
        this.proposal.continue();
        break;
      case 'encuesta':
        this.survey.continue();
        break;
      case 'pago':
        this.payment.requestConfirmation();
        break;
    }
  }

  public back(): void {
    if (!this.canGoBack()) return;
    if (this.currentStep() === 'encuesta') {
      this.survey.back();
      return;
    }
    if (this.currentStep() === 'pago') this.process.flow.previous();
  }

  public requestExit(): void {
    this.surveySaveError.set(null);
    this.exitConfirmationOpen.set(true);
  }

  public cancelExit(): void {
    this.surveySaveError.set(null);
    this.exitConfirmationOpen.set(false);
  }

  public confirmExit(): void {
    if (this.savingSurvey()) return;

    if (!this.survey.hasInitialSurveyRight()) {
      this.exitConfirmationOpen.set(false);
      void this.router.navigateByUrl('/inicio');
      return;
    }

    this.surveySaveError.set(null);
    this.savingSurvey.set(true);
    this.survey
      .savePartial()
      .pipe(
        finalize(() => this.savingSurvey.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: saved => {
          if (!saved) {
            this.surveySaveError.set('No se pudo guardar la encuesta. Intentá nuevamente.');
            return;
          }
          this.exitConfirmationOpen.set(false);
          void this.router.navigateByUrl('/inicio');
        },
        error: () =>
          this.surveySaveError.set('No se pudo guardar la encuesta. Intentá nuevamente.'),
      });
  }

  // Reintento manual (botón de la pantalla de error de encuesta): re-consulta el
  // estado de encuesta, re-deriva TODO el estado inicial con la nueva respuesta y lo
  // re-aplica. El backend vuelve a ganar sobre cualquier estado local.
  public retryInitialSurvey(): void {
    if (this.survey.loadingSurveyState()) return;
    this.survey
      .fetchResolvedInitialSurvey()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(survey => {
        this.entryContext = { ...this.entryContext, survey };
        this.applyInitialState(deriveInitialInscripcionState(this.entryContext));
      });
  }

  // Único punto de aplicación del estado inicial. Todo lo derivado (backend = fuente
  // de verdad) se aplica en orden determinístico; el paso del flujo se posiciona una
  // sola vez, al final, para que nada lo pise. Las facades hijas NO tocan el flujo.
  private applyInitialState(state: InscripcionInitialState): void {
    this.process.preEnrollmentResponse.set(state.preEnrollment);
    this.survey.applyInitialState(state.survey);
    if (state.resumeInProgress) this.proposal.disableForResume();
    this.applyPaymentInit(state.payment);
    this.process.flow.goTo(state.step);
  }

  private applyPaymentInit(payment: InscripcionPaymentInit): void {
    switch (payment.kind) {
      case 'none':
      case 'awaiting-method':
        return;
      case 'reserva':
        this.payment.selectedPaymentMethod.set(payment.method);
        this.payment.reservationData.set(payment.reservation);
        this.payment.outcome.set('reserva');
        return;
      case 'confirmada':
        this.payment.confirmedDetail.set(payment.detail);
        this.payment.outcome.set('inscription-confirmada');
        return;
      case 'en-proceso':
        this.payment.outcome.set('inscription-en-proceso');
        return;
    }
  }
}
