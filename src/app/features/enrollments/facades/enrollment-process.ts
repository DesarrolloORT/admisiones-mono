import { computed, DestroyRef, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { getAcademicProposalLevelIds } from '../../catalogs/models/academic-proposal';
import {
  deriveInitialEnrollmentState,
  type EnrollmentAcademicPrefill,
  type EnrollmentEntryContext,
  type EnrollmentEntryResolved,
  type EnrollmentInitialState,
  type EnrollmentInitialSurveyResolved,
  type EnrollmentPaymentInit,
} from '../models/enrollment-entry';
import { toNullableNumber } from '../models/enrollment-flow-mappers';
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
  public readonly backLabel = computed(() =>
    this.survey.readerOpen() ? 'Volver a Reglamento estudiantil' : 'Volver a la sección anterior'
  );
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

  private readonly flowMovement = computed(() => ({
    step: this.currentStep(),
    preEnrollment: this.process.preEnrollmentResponse(),
    paymentOutcome: this.payment.outcome(),
  }));

  private entryContext: EnrollmentEntryContext;
  private entryMovement = this.flowMovement();
  private urlIdentifiesEnrollment = false;

  constructor() {
    this.entryContext = {
      entry: this.route.snapshot.data['entry'] as EnrollmentEntryResolved,
      survey: this.route.snapshot.data['initialSurvey'] as EnrollmentInitialSurveyResolved,
    };
    this.applyInitialState(deriveInitialEnrollmentState(this.entryContext));
    effect(() => {
      const movement = this.flowMovement();
      if (movement === this.entryMovement || this.urlIdentifiesEnrollment) return;
      this.urlIdentifiesEnrollment = this.syncEnrollmentParams();
    });
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

  public exit(): void {
    this.survey
      .savePartial()
      .pipe(catchError(() => EMPTY))
      .subscribe();
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
    this.entryMovement = this.flowMovement();
    this.urlIdentifiesEnrollment = false;
  }

  private syncEnrollmentParams(): boolean {
    const controls = this.proposal.academicForm.controls;
    const productId = toNullableNumber(controls.degreeProgram.value);
    const admissionProcessId = toNullableNumber(controls.intake.value);
    if (productId === null || admissionProcessId === null) return false;

    // La app navega con `onSameUrlNavigation: 'reload'`: navegar a la MISMA URL recarga la
    // ruta y re-crea la página. Si los params ya identifican la inscripción y no arrastran
    // estado, no hay nada que reescribir.
    const params = this.route.snapshot.queryParamMap;
    if (
      !params.has('estado') &&
      !params.has('modo') &&
      params.get('idProducto') === String(productId) &&
      params.get('idProceso') === String(admissionProcessId)
    ) {
      return true;
    }

    const offeringIds = (
      this.proposal.selection.isProfessionalUpdate()
        ? controls.seminars.value
        : [controls.shift.value]
    ).filter(value => toNullableNumber(value) !== null);
    // `nivel` solo alimenta el tipo de propuesta al reingresar, y en Actualización
    // profesional cualquiera de sus niveles (3 o 4) elige el mismo tipo.
    const productLevelId = getAcademicProposalLevelIds(controls.proposalType.value)[0];

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        idProducto: productId,
        idProceso: admissionProcessId,
        ...(offeringIds.length > 0 ? { idOferta: offeringIds } : {}),
        ...(productLevelId ? { nivel: productLevelId } : {}),
        estado: null,
        modo: null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    return true;
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
