import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs/operators';

import { detailToPreEnrollment, type InscripcionDetail } from '../models/inscription-detail';
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

  constructor() {
    this.applyResumeContext();
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

  // Si la inscripción se retoma desde el panel con un detalle resuelto, reconstruye
  // el contexto y posiciona el flujo en el paso pendiente: "Pago pendiente" precarga
  // seña/vencimiento/resumen y salta al paso de pago; "Confirmada" precarga el
  // resumen y muestra el success step terminal. El resto sigue el flujo normal ya
  // configurado por la encuesta.
  private applyResumeContext(): void {
    const detail = this.route.snapshot.data['inscriptionDetail'] as InscripcionDetail | null;
    if (!detail) return;

    const preEnrollment = detailToPreEnrollment(detail);
    if (preEnrollment) this.process.preEnrollmentResponse.set(preEnrollment);

    if (detail.estado === 'Pago pendiente' || detail.estado === 'Pendiente') {
      this.process.flow.goTo('pago');
    } else if (detail.estado === 'Confirmada') {
      this.payment.outcome.set('inscription-confirmada');
    }
  }
}
