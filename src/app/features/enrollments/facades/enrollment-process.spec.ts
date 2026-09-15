import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import type { EnrollmentDetail } from '../models/enrollment-detail';
import {
  EMPTY_INITIAL_SURVEY_RESPONSE,
  type EnrollmentEntryResolved,
  type EnrollmentInitialSurveyResolved,
} from '../models/enrollment-entry';
import type { EnrollmentInitialSurvey, EnrollmentOfferingSummary } from '../models/enrollment-flow';
import {
  createEnrollmentProcessState,
  ENROLLMENT_PROCESS_STATE,
} from '../models/enrollment-process';
import { EnrollmentPaymentFacade } from './enrollment-payment';
import { EnrollmentProcessFacade } from './enrollment-process';
import { EnrollmentProposalFacade } from './enrollment-proposal';
import { EnrollmentSurveyFacade } from './enrollment-survey';

const NEW_ENTRY: EnrollmentEntryResolved = { intent: 'new' };
const FRESH: EnrollmentInitialSurveyResolved = {
  initialSurvey: { ...EMPTY_INITIAL_SURVEY_RESPONSE, survey: null },
  loadFailed: false,
};

describe('EnrollmentProcessFacade', () => {
  it('starts clean on a new enrollment', () => {
    const { process, payment, proposal, survey } = createFacade(NEW_ENTRY, FRESH);

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('proposal');
    expect(process.preEnrollmentResponse()).toBeNull();
    expect(payment.outcome()).toBeNull();
    expect(proposal.disableForResume).not.toHaveBeenCalled();
    expect(survey.applyInitialState).toHaveBeenCalledWith({ kind: 'fresh' });
  });

  it('keeps step 1 for a new enrollment even with an in-progress survey (bug regression)', () => {
    const { process, proposal, survey } = createFacade(
      NEW_ENTRY,
      inProgressSurvey('ort-experience')
    );

    TestBed.tick();

    // Empezar de cero SIEMPRE muestra el paso 1: la encuesta previa (por persona) no
    // reposiciona el flujo ni precarga la propuesta.
    expect(process.flow.currentStep()).toBe('proposal');
    expect(proposal.disableForResume).not.toHaveBeenCalled();
    expect(survey.applyInitialState).toHaveBeenCalledWith(
      expect.objectContaining({ kind: 'prefilled', includeAcademicSelection: false })
    );
  });

  it('resumes at the payment step with a pending-payment detail', () => {
    const { process } = createFacade(resume(createPendingPaymentDetail()), FRESH);

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('payment');
    expect(process.preEnrollmentResponse()).toEqual({
      enrollmentId: 1072704,
      confirmed: false,
      paymentDueDate: '2027-03-04',
      enrollmentDeposit: 15500,
      accountBalance: 1200,
      summary: { degreeProgram: 'Sistemas', intake: 'Marzo 2027', shift: 'Noche' },
      seminars: [],
    });
  });

  it('shows the reserva outcome without a payment method when the pending deposit is 0', () => {
    const { payment, process } = createFacade(resume(createZeroDepositPendingDetail()), FRESH);

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('payment');
    expect(payment.outcome()).toBe('reservation');
  });

  it('shows the terminal success outcome with a confirmed detail', () => {
    const { payment, process } = createFacade(resume(createConfirmedDetail()), FRESH);

    TestBed.tick();

    expect(process.preEnrollmentResponse()).toEqual({
      enrollmentId: null,
      confirmed: true,
      paymentDueDate: null,
      enrollmentDeposit: null,
      accountBalance: null,
      summary: { degreeProgram: 'Sistemas', intake: 'Marzo 2027', shift: 'Noche' },
      seminars: [],
    });
    expect(payment.outcome()).toBe('enrollment-confirmed');
    expect(payment.confirmedDetail()).toEqual(createConfirmedDetail().confirmed);
  });

  it('shows the payment references when the deposit method was already chosen', () => {
    const { payment, process } = createFacade(resume(createMinimumDepositDetail()), FRESH);

    TestBed.tick();

    // La pantalla de reserva se pinta por `outcome`; el paso queda en el 3 (nunca en el 1).
    expect(process.flow.currentStep()).toBe('payment');
    expect(payment.outcome()).toBe('reservation');
    expect(payment.selectedPaymentMethod()).toBe('abitab');
    expect(payment.reservationData()).toEqual({ documentNumber: '12345678', personCode: 555 });
    expect(process.preEnrollmentResponse()?.enrollmentDeposit).toBe(3339);
  });

  it('shows the in-process outcome when the enrollment is awaiting review', () => {
    const { payment } = createFacade(
      resume({
        status: 'A la espera',
        summary: null,
        interests: [],
        pendingPayment: null,
        minimumDeposit: null,
        confirmed: null,
      }),
      FRESH
    );

    TestBed.tick();

    expect(payment.outcome()).toBe('enrollment-in-progress');
  });

  it('resumes a Pendiente detail like a pending payment', () => {
    const { payment, process } = createFacade(
      resume({ ...createPendingPaymentDetail(), status: 'Pendiente' }),
      FRESH
    );

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('payment');
    expect(payment.outcome()).toBeNull();
    expect(process.preEnrollmentResponse()?.enrollmentId).toBe(1072704);
  });

  it('disables the survey proposal and lands on step 2 when resuming in progress', () => {
    const summary = {
      offeringId: 300,
      productId: 20,
      degreeProgram: 'Sistemas',
      intake: 'Marzo 2027',
      shift: 'Noche',
    };
    const { process, proposal, survey } = createFacade(
      resume({
        status: 'En proceso',
        summary,
        interests: [interest(300)],
        pendingPayment: null,
        minimumDeposit: null,
        confirmed: null,
      }),
      inProgressSurvey('education')
    );

    TestBed.tick();

    // Con precarga desde el Detalle, la selección académica de la encuesta no se aplica.
    expect(survey.applyInitialState).toHaveBeenCalledWith(
      expect.objectContaining({ kind: 'prefilled', includeAcademicSelection: false })
    );
    expect(proposal.disableForResume).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('survey');
  });

  it('prefills and locks step 1 from the detail when resuming an AP enrollment', () => {
    const summary = {
      offeringId: 310,
      productId: 21,
      degreeProgram: 'Programa de Asesoramiento Financiero',
      intake: 'Abril 2026',
      shift: 'Noche',
    };
    const { process, proposal } = createFacade(
      resume(
        {
          status: 'En proceso',
          summary,
          interests: [interest(310), interest(311)],
          pendingPayment: null,
          minimumDeposit: null,
          confirmed: null,
        },
        3
      ),
      FRESH
    );

    TestBed.tick();

    expect(proposal.academicForm.controls.proposalType.value).toBe('3');
    expect(proposal.academicForm.controls.degreeProgram.value).toBe('21');
    expect(proposal.academicForm.controls.shift.value).toBe('310');
    expect(proposal.academicForm.controls.seminars.value).toEqual(['310', '311']);
    expect(proposal.setProposalType).toHaveBeenCalledWith('3');
    expect(proposal.selection.loadSeminars).toHaveBeenCalledWith(21);
    expect(proposal.disableForResume).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('survey');
  });

  // Retomar siempre bloquea el paso 1, en cualquier status: la inscripción ya existe.
  it('locks step 1 on any resume, whatever the state of the enrollment', () => {
    const { proposal, process } = createFacade(resume(createPendingPaymentDetail()), FRESH);

    TestBed.tick();

    expect(proposal.disableForResume).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).not.toBe('proposal');
  });

  it('positions the flow step last, after applying proposal and survey slices', () => {
    const summary = {
      offeringId: 300,
      productId: 20,
      degreeProgram: 'Sistemas',
      intake: 'Marzo 2027',
      shift: 'Noche',
    };
    const { process, proposal, survey } = createFacade(
      resume({
        status: 'En proceso',
        summary,
        interests: [interest(300)],
        pendingPayment: null,
        minimumDeposit: null,
        confirmed: null,
      }),
      inProgressSurvey('education'),
      true,
      { spyGoTo: true }
    );

    TestBed.tick();

    const goToOrder = (process.flow.goTo as unknown as ReturnType<typeof vi.fn>).mock
      .invocationCallOrder[0];
    const proposalOrder = proposal.disableForResume.mock.invocationCallOrder[0];
    const surveyOrder = survey.applyInitialState.mock.invocationCallOrder[0];
    expect(goToOrder).toBeGreaterThan(proposalOrder);
    expect(goToOrder).toBeGreaterThan(surveyOrder);
  });

  it('re-derives and re-applies the state on a manual survey retry', () => {
    const { facade, survey } = createFacade(NEW_ENTRY, { initialSurvey: null, loadFailed: true });

    TestBed.tick();
    expect(survey.applyInitialState).toHaveBeenLastCalledWith({
      kind: 'load-failed',
      message: 'No se pudo consultar el estado de tu encuesta. Intentá nuevamente.',
    });

    survey.fetchResolvedInitialSurvey.mockReturnValue(of(FRESH));
    facade.retryInitialSurvey();

    expect(survey.fetchResolvedInitialSurvey).toHaveBeenCalledOnce();
    expect(survey.applyInitialState).toHaveBeenLastCalledWith({ kind: 'fresh' });
  });

  it('merges the proposal and survey catalog errors', () => {
    const { facade, proposal, survey } = createFacade(NEW_ENTRY, FRESH);

    expect(facade.catalogError()).toBeNull();

    survey.catalogError.set('Encuesta sin catálogos');
    expect(facade.catalogError()).toBe('Encuesta sin catálogos');

    proposal.catalogError.set('Propuesta sin catálogos');
    expect(facade.catalogError()).toBe('Propuesta sin catálogos');
  });

  // Sin este guardado se pierde lo respondido en una seccion que todavia no quedo completa:
  // el autoguardado por seccion solo dispara con la seccion valida.
  it('saves the pending survey delta on exit and navigates home', () => {
    const { facade, survey, router } = createFacade(NEW_ENTRY, FRESH);

    facade.exit();

    expect(survey.savePartial).toHaveBeenCalledOnce();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/inicio');
  });

  // El guardado es oportunista: un fallo no bloquea ni rompe la salida.
  it('still navigates home when the save on exit fails', () => {
    const { facade, survey, router } = createFacade(NEW_ENTRY, FRESH);
    survey.savePartial.mockReturnValue(throwError(() => new Error('network error')));

    facade.exit();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/inicio');
  });

  it('dispatches continue to the facade owning the current step', () => {
    const { facade, process, proposal, survey, payment } = createFacade(NEW_ENTRY, FRESH);

    facade.continue();
    expect(proposal.continue).toHaveBeenCalledOnce();

    process.flow.goTo('survey');
    facade.continue();
    expect(survey.continue).toHaveBeenCalledOnce();

    process.flow.goTo('payment');
    facade.continue();
    expect(payment.confirm).toHaveBeenCalledOnce();
  });

  // El flujo solo avanza: no se vuelve del paso 2 al 1 ni del 3 al 2.
  it('only allows going back inside step 2 and never changes the step', () => {
    const { facade, process, survey } = createFacade(NEW_ENTRY, FRESH);

    expect(facade.canGoBack()).toBe(false);
    facade.back();
    expect(survey.back).not.toHaveBeenCalled();
    expect(process.flow.currentStep()).toBe('proposal');

    // Paso 2, primera sección visible: nada hacia atrás.
    process.flow.goTo('survey');
    expect(facade.canGoBack()).toBe(false);
    facade.back();
    expect(survey.back).not.toHaveBeenCalled();

    // Paso 2, sección posterior: retrocede DENTRO del paso.
    survey.activeSection.set('identity');
    expect(facade.canGoBack()).toBe(true);
    facade.back();
    expect(survey.back).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('survey');

    // Lector de reglamento abierto en la primera sección: también retrocede.
    survey.activeSection.set('education');
    survey.readerOpen.set(true);
    expect(facade.canGoBack()).toBe(true);

    // Paso 3: nunca se vuelve al paso 2.
    survey.readerOpen.set(false);
    process.flow.goTo('payment');
    expect(facade.canGoBack()).toBe(false);
    facade.back();
    expect(process.flow.currentStep()).toBe('payment');
  });

  // El bug: recargar en el paso 3 volvía al paso 2 (el Detalle no encuentra `estado=En
  // proceso`) y ahí "Continuar" reconfirmaba una preinscripción ya confirmada (400, sin
  // salida). La URL debe identificar la inscripción, nunca su estado.
  it('rewrites the URL without the entry state when the flow advances a step', () => {
    const { process, router } = createFacade(resume(inProgressDetail(), 1), FRESH);

    TestBed.tick();
    expect(router.navigate).not.toHaveBeenCalled();

    process.flow.goTo('payment');
    TestBed.tick();

    expect(router.navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: {
          idProducto: 20,
          idProceso: 200,
          idOferta: ['300'],
          nivel: 1,
          estado: null,
          modo: null,
        },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      })
    );
  });

  // Seña 0, "a la espera" y corporativa cierran en pantalla terminal sin cambiar de paso:
  // la preinscripción ya está confirmada igual, así que la URL tampoco puede seguir
  // diciendo el estado con el que se entró.
  it('rewrites the URL when the pre-enrollment is confirmed without changing the step', () => {
    const { process, router } = createFacade(resume(inProgressDetail(), 1), FRESH);

    TestBed.tick();

    process.preEnrollmentResponse.set({
      confirmed: true,
      isWaiting: true,
      enrollmentId: 1072704,
      paymentDueDate: null,
      enrollmentDeposit: 0,
      accountBalance: null,
      summary: null,
      seminars: [],
    });
    TestBed.tick();

    expect(router.navigate).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('survey');
  });

  // Una inscripción nueva no llega con params: sin esto, recargar después de avanzar
  // volvía al paso 1 con el interés ya registrado.
  it('adds the enrollment params on a new enrollment as soon as the flow advances', () => {
    const { process, proposal, router } = createFacade(NEW_ENTRY, FRESH);

    TestBed.tick();

    proposal.academicForm.setValue({
      proposalType: '1',
      degreeProgram: '20',
      intake: '200',
      shift: '300',
      seminars: [],
    });
    process.flow.next();
    TestBed.tick();

    expect(router.navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({
        queryParams: {
          idProducto: 20,
          idProceso: 200,
          idOferta: ['300'],
          nivel: 1,
          estado: null,
          modo: null,
        },
      })
    );
  });

  // La app navega con `onSameUrlNavigation: 'reload'`: navegar a la MISMA URL recargaría la
  // ruta y re-crearía la página en medio del flujo.
  it('does not navigate when the URL already identifies the enrollment without its state', () => {
    const { process, router } = createFacade(resume(inProgressDetail(), 1), FRESH, true, {
      queryParams: { idProducto: '20', idProceso: '200', idOferta: '300', nivel: '1' },
    });

    TestBed.tick();
    process.flow.goTo('payment');
    TestBed.tick();

    expect(router.navigate).not.toHaveBeenCalled();
  });

  // Re-derivar por el retry de encuesta no es movimiento del flujo: `estado` sigue
  // describiendo la inscripción con la que se entró y no hay que perderlo.
  it('does not rewrite the URL when the initial state is re-derived on a survey retry', () => {
    const { facade, survey, router } = createFacade(resume(inProgressDetail(), 1), {
      initialSurvey: null,
      loadFailed: true,
    });

    TestBed.tick();
    survey.fetchResolvedInitialSurvey.mockReturnValue(of(FRESH));
    facade.retryInitialSurvey();
    TestBed.tick();

    expect(router.navigate).not.toHaveBeenCalled();
  });

  // Re-derivar puede no mover ninguna señal del flujo (inscripción nueva: sigue en el paso 1
  // sin preinscripción). El punto de entrada no puede saltearse el primer avance posterior.
  it('still rewrites the URL on the first advance after a retry that moved nothing', () => {
    const { facade, process, proposal, survey, router } = createFacade(NEW_ENTRY, {
      initialSurvey: null,
      loadFailed: true,
    });

    TestBed.tick();
    survey.fetchResolvedInitialSurvey.mockReturnValue(of(FRESH));
    facade.retryInitialSurvey();
    TestBed.tick();

    proposal.academicForm.setValue({
      proposalType: '1',
      degreeProgram: '20',
      intake: '200',
      shift: '300',
      seminars: [],
    });
    process.flow.next();
    TestBed.tick();

    expect(router.navigate).toHaveBeenCalledOnce();
  });

  it('blocks back while the payment is processing or being edited', () => {
    const { facade, process, payment } = createFacade(NEW_ENTRY, FRESH);
    process.flow.goTo('payment');
    payment.view.set('processing');

    facade.back();

    expect(facade.canGoBack()).toBe(false);
    expect(process.flow.currentStep()).toBe('payment');

    payment.view.set('editing');
    expect(facade.canGoBack()).toBe(false);
  });

  it('describes the current step and back label per state', () => {
    const { facade, process, survey } = createFacade(NEW_ENTRY, FRESH);

    expect(facade.stepLabel()).toBe('Paso 1 de 3 - Propuesta académica');

    process.flow.goTo('survey');
    expect(facade.stepLabel()).toBe('Paso 2 de 3 - Información personal');
    expect(facade.backLabel()).toBe('Volver a la sección anterior');

    survey.readerOpen.set(true);
    expect(facade.backLabel()).toBe('Volver a Reglamento estudiantil');

    survey.readerOpen.set(false);
    process.flow.goTo('payment');
    expect(facade.stepLabel()).toBe('Paso 3 de 3 - Confirmación');
  });

  it('hides the stepper while reading the regulation, on terminal outcomes and while processing', () => {
    const { facade, payment, survey } = createFacade(NEW_ENTRY, FRESH);

    expect(facade.showStepper()).toBe(true);

    survey.readerOpen.set(true);
    expect(facade.showStepper()).toBe(false);

    survey.readerOpen.set(false);
    payment.view.set('processing');
    expect(facade.showStepper()).toBe(false);
    expect(facade.canGoBack()).toBe(false);

    payment.view.set('editing');
    payment.outcome.set('reservation');
    expect(facade.showStepper()).toBe(false);
    expect(facade.canGoBack()).toBe(false);
  });
});

function resume(
  detail: EnrollmentDetail,
  productLevelId: number | null = null
): EnrollmentEntryResolved {
  return {
    intent: 'resume',
    detail,
    productId: 20,
    admissionProcessId: 200,
    offeringIds: [],
    productLevelId,
  };
}

function inProgressDetail(): EnrollmentDetail {
  return {
    status: 'En proceso',
    summary: {
      offeringId: 300,
      productId: 20,
      degreeProgram: 'Sistemas',
      intake: 'Marzo 2027',
      shift: 'Noche',
    },
    interests: [interest(300)],
    pendingPayment: null,
    minimumDeposit: null,
    confirmed: null,
  };
}

function interest(offeringId: number): EnrollmentOfferingSummary {
  return { enrollmentId: null, offeringId, name: 'Oferta', intake: null, shift: null };
}

function inProgressSurvey(
  activeSection: EnrollmentInitialSurvey['activeSection']
): EnrollmentInitialSurveyResolved {
  return {
    initialSurvey: {
      ...EMPTY_INITIAL_SURVEY_RESPONSE,
      survey: createInitialSurvey({ activeSection }),
    },
    loadFailed: false,
  };
}

function createFacade(
  entry: EnrollmentEntryResolved,
  surveyResolved: EnrollmentInitialSurveyResolved,
  initialized = true,
  options: { spyGoTo?: boolean; queryParams?: Record<string, string> } = {}
) {
  const payment = {
    outcome: signal<string | null>(null),
    view: signal('editing'),
    confirmedDetail: signal(null),
    reservationData: signal<{ documentNumber: string | null; personCode: number | null } | null>(
      null
    ),
    selectedPaymentMethod: signal(null),
    confirm: vi.fn(),
  };
  const proposal = {
    initialized: signal(initialized),
    catalogError: signal<string | null>(null),
    academicForm: new FormGroup({
      proposalType: new FormControl('', { nonNullable: true }),
      degreeProgram: new FormControl('', { nonNullable: true }),
      intake: new FormControl('', { nonNullable: true }),
      shift: new FormControl('', { nonNullable: true }),
      seminars: new FormControl<string[]>([], { nonNullable: true }),
    }),
    setProposalType: vi.fn(),
    selection: {
      loadSeminars: vi.fn(),
      // Refleja al servicio real: el tipo de propuesta del form manda.
      isProfessionalUpdate: () => proposal.academicForm.controls.proposalType.value === '3',
    },
    continue: vi.fn(),
    disableForResume: vi.fn(),
  };
  const activeSection = signal('education');
  const readerOpen = signal(false);
  const visibleSections = signal(['education', 'identity', 'reglamento']);
  const survey = {
    initialized: signal(initialized),
    activeSection,
    readerOpen,
    visibleSections,
    canGoBack: computed(() => readerOpen() || visibleSections().indexOf(activeSection()) > 0),
    catalogError: signal<string | null>(null),
    loadingSurveyState: signal(false),
    surveyLoadError: signal<string | null>(null),
    canAnswerSurvey: signal(true),
    back: vi.fn(),
    continue: vi.fn(),
    savePartial: vi.fn().mockReturnValue(of(undefined)),
    applyInitialState: vi.fn(),
    fetchResolvedInitialSurvey: vi.fn(),
  };
  const router = { navigateByUrl: vi.fn(), navigate: vi.fn() };

  TestBed.configureTestingModule({
    providers: [
      EnrollmentProcessFacade,
      { provide: ENROLLMENT_PROCESS_STATE, useFactory: createEnrollmentProcessState },
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: {
            data: { entry, initialSurvey: surveyResolved },
            queryParamMap: convertToParamMap(options.queryParams ?? {}),
          },
        },
      },
      { provide: Router, useValue: router },
      { provide: EnrollmentProposalFacade, useValue: proposal },
      { provide: EnrollmentSurveyFacade, useValue: survey },
      { provide: EnrollmentPaymentFacade, useValue: payment },
    ],
  });

  const process = TestBed.inject(ENROLLMENT_PROCESS_STATE);
  if (options.spyGoTo) vi.spyOn(process.flow, 'goTo');
  const facade = TestBed.inject(EnrollmentProcessFacade);

  return { facade, payment, process, proposal, survey, router };
}

function createInitialSurvey(
  values: Partial<EnrollmentInitialSurvey> = {}
): EnrollmentInitialSurvey {
  return {
    degreeProgramId: null,
    intakeId: null,
    shiftId: null,
    productLevelId: null,
    complete: false,
    activeSection: null,
    studiesHighSchool: null,
    highSchoolOrientationId: null,
    highSchoolYearId: null,
    repeatsHighSchoolYear: null,
    highSchoolYearRepeatCount: null,
    highSchoolInstitutionId: null,
    highSchoolInstitutionStateId: null,
    highSchoolLocationId: null,
    highSchoolInstitutionName: null,
    priorHigherEducationStatusId: null,
    motherEducationLevelId: null,
    fatherEducationLevelId: null,
    isMotherOrtGraduate: null,
    isFatherOrtGraduate: null,
    degreeProgramDecisionYearId: null,
    ortDecisionYearId: null,
    researchedOtherUniversities: null,
    decisionSupportId: null,
    decisionLevelId: null,
    hadOrtAdvising: null,
    ortAdvisingRating: null,
    visitedOrtWebsite: null,
    ortWebsiteRating: null,
    visitedOrtCampus: null,
    ortCampusRating: null,
    recallsOrtAdvertising: null,
    ...values,
  };
}

function createPendingPaymentDetail(): EnrollmentDetail {
  return {
    status: 'Pago pendiente',
    summary: null,
    interests: [],
    pendingPayment: {
      enrollmentId: 1072704,
      deposit: 15500,
      accountBalance: 1200,
      paymentDueDate: '2027-03-04',
      summary: {
        offeringId: 300,
        productId: 20,
        degreeProgram: 'Sistemas',
        intake: 'Marzo 2027',
        shift: 'Noche',
      },
      seminars: [],
    },
    minimumDeposit: null,
    confirmed: null,
  };
}

function createZeroDepositPendingDetail(): EnrollmentDetail {
  return {
    ...createPendingPaymentDetail(),
    pendingPayment: { ...createPendingPaymentDetail().pendingPayment!, deposit: 0 },
  };
}

function createConfirmedDetail(): EnrollmentDetail {
  return {
    status: 'Confirmada',
    summary: null,
    interests: [],
    pendingPayment: null,
    minimumDeposit: null,
    confirmed: {
      studentNumber: 397654,
      summary: {
        offeringId: 300,
        productId: 20,
        degreeProgram: 'Sistemas',
        intake: 'Marzo 2027',
        shift: 'Noche',
      },
      academicCoordinator: null,
      courseCoordinator: null,
      enrollments: [],
    },
  };
}

function createMinimumDepositDetail(): EnrollmentDetail {
  return {
    status: 'Pago pendiente',
    summary: null,
    interests: [],
    pendingPayment: null,
    minimumDeposit: {
      paymentMethod: 'ABITAB',
      documentNumber: '12345678',
      personCode: 555,
      deposit: 3339,
    },
    confirmed: null,
  };
}
