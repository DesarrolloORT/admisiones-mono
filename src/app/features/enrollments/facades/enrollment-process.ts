import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import {
  deriveInitialEnrollmentState,
  type EnrollmentAcademicPrefill,
  type EnrollmentEntryContext,
  type EnrollmentEntryResolved,
  type EnrollmentInitialState,
  type EnrollmentInitialSurveyResolved,
  type EnrollmentPaymentInit,
} from '../models/enrollment-entry';
import { ENROLLMENT_PROCESS_STATE } from '../models/enrollment-process';
import { EnrollmentPaymentFacade } from './enrollment-payment';
import { EnrollmentProposalFacade } from './enrollment-proposal';
import { EnrollmentSurveyFacade } from './enrollment-survey';

export class EnrollmentProcessFacade {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly process = inject(ENROLLMENT_PROCESS_STATE);

  public readonly proposal = inject(EnrollmentProposalFacade);
  public readonly survey = inject(EnrollmentSurveyFacade);
  public readonly payment = inject(EnrollmentPaymentFacade);

  public readonly currentStep = this.process.flow.currentStep;
  public readonly stepItems = computed(() => [...this.process.flow.stepItems()]);
  public readonly stepNumber = computed(() => this.process.flow.currentIndex() + 1);
  public readonly stepLabel = computed(() => {
    const step = this.stepItems()[this.process.flow.currentIndex()];
    return `Paso ${this.stepNumber()} de ${this.stepItems().length} - ${step.title}`;
  });
  // Solo se retrocede DENTRO del paso 2 (ver `canGoBack`), así que no hay etiquetas
  // "volver al paso N".
  public readonly backLabel = computed(() =>
    this.survey.readerOpen() ? 'Volver a Reglamento estudiantil' : 'Volver a la sección anterior'
  );
  public readonly exitConfirmationOpen = signal(false);
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
  // El flujo solo avanza: avanzar de paso es un hecho de negocio ya registrado en el
  // backend (paso 1 ⇒ interés de producto, paso 2 ⇒ preinscripción), así que
  // nunca se vuelve a un paso anterior. Lo único que retrocede son las sub-secciones
  // del paso 2 y el lector de reglamento.
  public readonly canGoBack = computed(() => {
    if (this.payment.outcome() || this.payment.view() === 'processing') return false;
    return this.currentStep() === 'survey' && this.survey.canGoBack();
  });

  private entryContext: EnrollmentEntryContext;

  constructor() {
    this.entryContext = {
      entry: this.route.snapshot.data['entry'] as EnrollmentEntryResolved,
      survey: this.route.snapshot.data['initialSurvey'] as EnrollmentInitialSurveyResolved,
    };
    this.applyInitialState(deriveInitialEnrollmentState(this.entryContext));
  }

  public continue(): void {
    switch (this.currentStep()) {
      case 'proposal':
        this.proposal.continue();
        break;
      case 'survey':
        this.survey.continue();
        break;
      case 'payment':
        this.payment.confirm();
        break;
    }
  }

  public back(): void {
    if (this.canGoBack()) this.survey.back();
  }

  public requestExit(): void {
    this.exitConfirmationOpen.set(true);
  }

  public cancelExit(): void {
    this.exitConfirmationOpen.set(false);
  }

  public confirmExit(): void {
    this.exitConfirmationOpen.set(false);
    this.router.navigateByUrl('/inicio');
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
        this.applyInitialState(deriveInitialEnrollmentState(this.entryContext));
      });
  }

  // Único punto de aplicación del estado inicial. Todo lo derivado (backend = fuente
  // de verdad) se aplica en orden determinístico; el paso del flujo se posiciona una
  // sola vez, al final, para que nada lo pise. Las facades hijas NO tocan el flujo.
  private applyInitialState(state: EnrollmentInitialState): void {
    this.process.preEnrollmentResponse.set(state.preEnrollment);
    this.survey.applyInitialState(state.survey);
    // Después de aplicar la encuesta: si la persona tiene una encuesta previa de
    // otro flujo, el paso 1 debe quedar con el programa del Detalle, no con la
    // carrera de esa encuesta.
    if (state.academicPrefill) this.applyAcademicPrefill(state.academicPrefill);
    if (state.resumeInProgress) this.proposal.disableForResume();
    this.applyPaymentInit(state.payment);
    this.process.flow.goTo(state.step);
  }

  private applyAcademicPrefill(prefill: EnrollmentAcademicPrefill): void {
    const controls = this.proposal.academicForm.controls;
    controls.proposalType.setValue(prefill.proposalType, { emitEvent: false });
    controls.degreeProgram.setValue(prefill.degreeProgram, { emitEvent: false });
    controls.intake.setValue(prefill.intake, { emitEvent: false });
    controls.shift.setValue(prefill.shift, { emitEvent: false });
    controls.seminars.setValue([...prefill.seminars], { emitEvent: false });
    this.proposal.setProposalType(prefill.proposalType);
    const programId = Number(prefill.degreeProgram);
    if (Number.isFinite(programId) && this.proposal.selection.isProfessionalUpdate()) {
      this.proposal.selection.loadSeminars(programId);
    }
  }

  private applyPaymentInit(payment: EnrollmentPaymentInit): void {
    switch (payment.kind) {
      case 'none':
        return;
      case 'awaiting-method':
        // Seña 0: no hay nada que cobrar, se salta la elección de medio de pago.
        if (this.process.preEnrollmentResponse()?.enrollmentDeposit === 0) {
          this.payment.outcome.set('reservation');
        }
        return;
      case 'reservation':
        this.payment.selectedPaymentMethod.set(payment.method);
        this.payment.reservationData.set(payment.reservation);
        this.payment.outcome.set('reservation');
        return;
      case 'confirmed':
        this.payment.confirmedDetail.set(payment.detail);
        this.payment.outcome.set('enrollment-confirmed');
        return;
      case 'in-progress':
        this.payment.outcome.set('enrollment-in-progress');
        return;
    }
  }
}
