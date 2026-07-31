import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import type { InscripcionDetail } from '../models/inscription-detail';
import {
  EMPTY_INITIAL_SURVEY_RESPONSE,
  type InscripcionEntryResolved,
  type InscripcionInitialSurveyResolved,
} from '../models/inscription-entry';
import type {
  InscripcionInitialSurvey,
  InscripcionOfertaResumen,
} from '../models/inscription-flow';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProcessFacade } from './inscription-process';
import { InscripcionProposalFacade } from './inscription-proposal';
import { InscripcionSurveyFacade } from './inscription-survey';

const NUEVA: InscripcionEntryResolved = { intent: 'nueva' };
const FRESH: InscripcionInitialSurveyResolved = {
  initialSurvey: { ...EMPTY_INITIAL_SURVEY_RESPONSE, encuesta: null },
  loadFailed: false,
};

describe('InscripcionProcessFacade', () => {
  it('starts clean on a new inscription', () => {
    const { process, payment, proposal, survey } = createFacade(NUEVA, FRESH);

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('propuesta');
    expect(process.preEnrollmentResponse()).toBeNull();
    expect(payment.outcome()).toBeNull();
    expect(proposal.disableForResume).not.toHaveBeenCalled();
    expect(survey.applyInitialState).toHaveBeenCalledWith({ kind: 'fresh' });
  });

  it('keeps step 1 for a new inscription even with an in-progress survey (bug regression)', () => {
    const { process, proposal, survey } = createFacade(NUEVA, inProgressSurvey('experiencia-ort'));

    TestBed.tick();

    // Empezar de cero SIEMPRE muestra el paso 1: la encuesta previa (por persona) no
    // reposiciona el flujo ni precarga la propuesta.
    expect(process.flow.currentStep()).toBe('propuesta');
    expect(proposal.disableForResume).not.toHaveBeenCalled();
    expect(survey.applyInitialState).toHaveBeenCalledWith(
      expect.objectContaining({ kind: 'prefilled', includeAcademicSelection: false })
    );
  });

  it('resumes at the payment step with a pending-payment detail', () => {
    const { process } = createFacade(retomar(createPendingPaymentDetail()), FRESH);

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('pago');
    expect(process.preEnrollmentResponse()).toEqual({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: '2027-03-04',
      seniaInscripcion: 15500,
      saldoCuenta: 1200,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
      seminarios: [],
    });
  });

  it('shows the terminal success outcome with a confirmed detail', () => {
    const { payment, process } = createFacade(retomar(createConfirmedDetail()), FRESH);

    TestBed.tick();

    expect(process.preEnrollmentResponse()).toEqual({
      idInscripcion: null,
      confirmada: true,
      fechaVencimientoPago: null,
      seniaInscripcion: null,
      saldoCuenta: null,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
      seminarios: [],
    });
    expect(payment.outcome()).toBe('inscription-confirmada');
    expect(payment.confirmedDetail()).toEqual(createConfirmedDetail().confirmada);
  });

  it('shows the payment references when the deposit method was already chosen', () => {
    const { payment, process } = createFacade(retomar(createSeniaMinimaDetail()), FRESH);

    TestBed.tick();

    // La pantalla de reserva se pinta por `outcome`; el paso queda en el 3 (nunca en el 1).
    expect(process.flow.currentStep()).toBe('pago');
    expect(payment.outcome()).toBe('reserva');
    expect(payment.selectedPaymentMethod()).toBe('abitab');
    expect(payment.reservationData()).toEqual({ cedula: '12345678', codigoPersona: 555 });
    expect(process.preEnrollmentResponse()?.seniaInscripcion).toBe(3339);
  });

  it('shows the in-process outcome when the enrollment is awaiting review', () => {
    const { payment } = createFacade(
      retomar({
        estado: 'A la espera',
        detalle: null,
        intereses: [],
        pagoPendiente: null,
        seniaMinima: null,
        confirmada: null,
      }),
      FRESH
    );

    TestBed.tick();

    expect(payment.outcome()).toBe('inscription-en-proceso');
  });

  it('resumes a Pendiente detail like a pending payment', () => {
    const { payment, process } = createFacade(
      retomar({ ...createPendingPaymentDetail(), estado: 'Pendiente' }),
      FRESH
    );

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('pago');
    expect(payment.outcome()).toBeNull();
    expect(process.preEnrollmentResponse()?.idInscripcion).toBe(1072704);
  });

  it('disables the survey proposal and lands on step 2 when resuming in progress', () => {
    const detalle = {
      idOferta: 300,
      idProducto: 20,
      carrera: 'Sistemas',
      comienzo: 'Marzo 2027',
      turno: 'Noche',
    };
    const { process, proposal, survey } = createFacade(
      retomar({
        estado: 'En proceso',
        detalle,
        intereses: [interes(300)],
        pagoPendiente: null,
        seniaMinima: null,
        confirmada: null,
      }),
      inProgressSurvey('educacion')
    );

    TestBed.tick();

    // Con precarga desde el Detalle, la selección académica de la encuesta no se aplica.
    expect(survey.applyInitialState).toHaveBeenCalledWith(
      expect.objectContaining({ kind: 'prefilled', includeAcademicSelection: false })
    );
    expect(proposal.disableForResume).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('encuesta');
  });

  it('prefills and locks step 1 from the detail when resuming an AP inscription', () => {
    const detalle = {
      idOferta: 310,
      idProducto: 21,
      carrera: 'Programa de Asesoramiento Financiero',
      comienzo: 'Abril 2026',
      turno: 'Noche',
    };
    const { process, proposal } = createFacade(
      retomar(
        {
          estado: 'En proceso',
          detalle,
          intereses: [interes(310), interes(311)],
          pagoPendiente: null,
          seniaMinima: null,
          confirmada: null,
        },
        3
      ),
      FRESH
    );

    TestBed.tick();

    expect(proposal.academicForm.controls.tipoPropuesta.value).toBe('3');
    expect(proposal.academicForm.controls.carrera.value).toBe('21');
    expect(proposal.academicForm.controls.turno.value).toBe('310');
    expect(proposal.academicForm.controls.seminarios.value).toEqual(['310', '311']);
    expect(proposal.setProposalType).toHaveBeenCalledWith('3');
    expect(proposal.selection.loadSeminars).toHaveBeenCalledWith(21);
    expect(proposal.disableForResume).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('encuesta');
  });

  // Retomar siempre bloquea el paso 1, en cualquier estado: la inscripción ya existe.
  it('locks step 1 on any resume, whatever the state of the inscription', () => {
    const { proposal, process } = createFacade(retomar(createPendingPaymentDetail()), FRESH);

    TestBed.tick();

    expect(proposal.disableForResume).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).not.toBe('propuesta');
  });

  it('positions the flow step last, after applying proposal and survey slices', () => {
    const detalle = {
      idOferta: 300,
      idProducto: 20,
      carrera: 'Sistemas',
      comienzo: 'Marzo 2027',
      turno: 'Noche',
    };
    const { process, proposal, survey } = createFacade(
      retomar({
        estado: 'En proceso',
        detalle,
        intereses: [interes(300)],
        pagoPendiente: null,
        seniaMinima: null,
        confirmada: null,
      }),
      inProgressSurvey('educacion'),
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
    const { facade, survey } = createFacade(NUEVA, { initialSurvey: null, loadFailed: true });

    TestBed.tick();
    expect(survey.applyInitialState).toHaveBeenLastCalledWith({ kind: 'load-failed' });

    survey.fetchResolvedInitialSurvey.mockReturnValue(of(FRESH));
    facade.retryInitialSurvey();

    expect(survey.fetchResolvedInitialSurvey).toHaveBeenCalledOnce();
    expect(survey.applyInitialState).toHaveBeenLastCalledWith({ kind: 'fresh' });
  });

  it('merges the proposal and survey catalog errors', () => {
    const { facade, proposal, survey } = createFacade(NUEVA, FRESH);

    expect(facade.catalogError()).toBeNull();

    survey.catalogError.set('Encuesta sin catálogos');
    expect(facade.catalogError()).toBe('Encuesta sin catálogos');

    proposal.catalogError.set('Propuesta sin catálogos');
    expect(facade.catalogError()).toBe('Propuesta sin catálogos');
  });

  it('opens and closes the exit confirmation dialog', () => {
    const { facade } = createFacade(NUEVA, FRESH);

    facade.requestExit();
    expect(facade.exitConfirmationOpen()).toBe(true);

    facade.cancelExit();
    expect(facade.exitConfirmationOpen()).toBe(false);
    expect(facade.surveySaveError()).toBeNull();
  });

  it('exits without saving when the user has no initial survey right', () => {
    const { facade, survey, router } = createFacade(NUEVA, FRESH);
    survey.hasInitialSurveyRight.set(false);

    facade.requestExit();
    facade.confirmExit();

    expect(survey.savePartial).not.toHaveBeenCalled();
    expect(facade.exitConfirmationOpen()).toBe(false);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/inicio');
  });

  it('saves the partial survey and navigates home on exit', () => {
    const { facade, survey, router } = createFacade(NUEVA, FRESH);

    facade.requestExit();
    facade.confirmExit();

    expect(survey.savePartial).toHaveBeenCalledOnce();
    expect(facade.exitConfirmationOpen()).toBe(false);
    expect(facade.surveySaveError()).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/inicio');
  });

  it('shows an error and stays when the partial save reports failure', () => {
    const { facade, survey, router } = createFacade(NUEVA, FRESH);
    survey.savePartial.mockReturnValue(of(false));

    facade.requestExit();
    facade.confirmExit();

    expect(facade.surveySaveError()).toBe('No se pudo guardar la encuesta. Intentá nuevamente.');
    expect(facade.exitConfirmationOpen()).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('shows an error and stays when the partial save fails', () => {
    const { facade, survey, router } = createFacade(NUEVA, FRESH);
    survey.savePartial.mockReturnValue(throwError(() => new Error('offline')));

    facade.requestExit();
    facade.confirmExit();

    expect(facade.surveySaveError()).toBe('No se pudo guardar la encuesta. Intentá nuevamente.');
    expect(facade.exitConfirmationOpen()).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('ignores a second exit confirmation while the survey is saving', () => {
    const { facade, survey, router } = createFacade(NUEVA, FRESH);
    const saving = new Subject<boolean>();
    survey.savePartial.mockReturnValue(saving.asObservable());

    facade.requestExit();
    facade.confirmExit();
    facade.confirmExit();

    expect(survey.savePartial).toHaveBeenCalledTimes(1);
    saving.next(true);
    saving.complete();
    expect(router.navigateByUrl).toHaveBeenCalledTimes(1);
    expect(facade.exitConfirmationOpen()).toBe(false);
  });

  it('dispatches continue to the facade owning the current step', () => {
    const { facade, process, proposal, survey, payment } = createFacade(NUEVA, FRESH);

    facade.continue();
    expect(proposal.continue).toHaveBeenCalledOnce();

    process.flow.goTo('encuesta');
    facade.continue();
    expect(survey.continue).toHaveBeenCalledOnce();

    process.flow.goTo('pago');
    facade.continue();
    expect(payment.requestConfirmation).toHaveBeenCalledOnce();
  });

  // El flujo solo avanza: no se vuelve del paso 2 al 1 ni del 3 al 2.
  it('only allows going back inside step 2 and never changes the step', () => {
    const { facade, process, survey } = createFacade(NUEVA, FRESH);

    expect(facade.canGoBack()).toBe(false);
    facade.back();
    expect(survey.back).not.toHaveBeenCalled();
    expect(process.flow.currentStep()).toBe('propuesta');

    // Paso 2, primera sección visible: nada hacia atrás.
    process.flow.goTo('encuesta');
    expect(facade.canGoBack()).toBe(false);
    facade.back();
    expect(survey.back).not.toHaveBeenCalled();

    // Paso 2, sección posterior: retrocede DENTRO del paso.
    survey.activeSection.set('identidad');
    expect(facade.canGoBack()).toBe(true);
    facade.back();
    expect(survey.back).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('encuesta');

    // Lector de reglamento abierto en la primera sección: también retrocede.
    survey.activeSection.set('educacion');
    survey.readerOpen.set(true);
    expect(facade.canGoBack()).toBe(true);

    // Paso 3: nunca se vuelve al paso 2.
    survey.readerOpen.set(false);
    process.flow.goTo('pago');
    expect(facade.canGoBack()).toBe(false);
    facade.back();
    expect(process.flow.currentStep()).toBe('pago');
  });

  it('blocks back while the payment is processing or being edited', () => {
    const { facade, process, payment } = createFacade(NUEVA, FRESH);
    process.flow.goTo('pago');
    payment.view.set('processing');

    facade.back();

    expect(facade.canGoBack()).toBe(false);
    expect(process.flow.currentStep()).toBe('pago');

    payment.view.set('editing');
    expect(facade.canGoBack()).toBe(false);
  });

  it('describes the current step and back label per state', () => {
    const { facade, process, survey } = createFacade(NUEVA, FRESH);

    expect(facade.stepLabel()).toBe('Paso 1 de 3 - Propuesta académica');

    process.flow.goTo('encuesta');
    expect(facade.stepLabel()).toBe('Paso 2 de 3 - Información personal');
    expect(facade.backLabel()).toBe('Volver a la sección anterior');

    survey.readerOpen.set(true);
    expect(facade.backLabel()).toBe('Volver a Reglamento estudiantil');

    survey.readerOpen.set(false);
    process.flow.goTo('pago');
    expect(facade.stepLabel()).toBe('Paso 3 de 3 - Confirmación');
  });

  it('hides the stepper while reading the regulation, on terminal outcomes and while processing', () => {
    const { facade, payment, survey } = createFacade(NUEVA, FRESH);

    expect(facade.showStepper()).toBe(true);

    survey.readerOpen.set(true);
    expect(facade.showStepper()).toBe(false);

    survey.readerOpen.set(false);
    payment.view.set('processing');
    expect(facade.showStepper()).toBe(false);
    expect(facade.canGoBack()).toBe(false);

    payment.view.set('editing');
    payment.outcome.set('reserva');
    expect(facade.showStepper()).toBe(false);
    expect(facade.canGoBack()).toBe(false);
  });
});

function retomar(
  detail: InscripcionDetail,
  idNivelProducto: number | null = null
): InscripcionEntryResolved {
  return { intent: 'retomar', detail, idProducto: 20, idProceso: 200, idNivelProducto };
}

function interes(idOferta: number): InscripcionOfertaResumen {
  return { idInscripcion: null, idOferta, nombre: 'Oferta', comienzo: null, turno: null };
}

function inProgressSurvey(
  seccionActiva: InscripcionInitialSurvey['seccionActiva']
): InscripcionInitialSurveyResolved {
  return {
    initialSurvey: {
      ...EMPTY_INITIAL_SURVEY_RESPONSE,
      encuesta: createInitialSurvey({ seccionActiva }),
    },
    loadFailed: false,
  };
}

function createFacade(
  entry: InscripcionEntryResolved,
  surveyResolved: InscripcionInitialSurveyResolved,
  initialized = true,
  options: { spyGoTo?: boolean } = {}
) {
  const payment = {
    outcome: signal<string | null>(null),
    view: signal('editing'),
    confirmedDetail: signal(null),
    reservationData: signal<{ cedula: string | null; codigoPersona: number | null } | null>(null),
    selectedPaymentMethod: signal(null),
    requestConfirmation: vi.fn(),
  };
  const proposal = {
    initialized: signal(initialized),
    catalogError: signal<string | null>(null),
    academicForm: new FormGroup({
      tipoPropuesta: new FormControl('', { nonNullable: true }),
      carrera: new FormControl('', { nonNullable: true }),
      comienzo: new FormControl('', { nonNullable: true }),
      turno: new FormControl('', { nonNullable: true }),
      seminarios: new FormControl<string[]>([], { nonNullable: true }),
    }),
    setProposalType: vi.fn(),
    selection: {
      loadSeminars: vi.fn(),
      // Refleja al servicio real: el tipo de propuesta del form manda.
      isProfessionalUpdate: () => proposal.academicForm.controls.tipoPropuesta.value === '3',
    },
    continue: vi.fn(),
    disableForResume: vi.fn(),
  };
  const activeSection = signal('educacion');
  const readerOpen = signal(false);
  const visibleSections = signal(['educacion', 'identidad', 'reglamento']);
  const survey = {
    initialized: signal(initialized),
    activeSection,
    readerOpen,
    scenario: signal('primera-vez'),
    visibleSections,
    canGoBack: computed(() => readerOpen() || visibleSections().indexOf(activeSection()) > 0),
    catalogError: signal<string | null>(null),
    loadingSurveyState: signal(false),
    surveyLoadError: signal<string | null>(null),
    hasInitialSurveyRight: signal(true),
    back: vi.fn(),
    continue: vi.fn(),
    savePartial: vi.fn().mockReturnValue(of(true)),
    applyInitialState: vi.fn(),
    fetchResolvedInitialSurvey: vi.fn(),
  };
  const router = { navigateByUrl: vi.fn() };

  TestBed.configureTestingModule({
    providers: [
      InscripcionProcessFacade,
      InscripcionProcessStore,
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { data: { entry, initialSurvey: surveyResolved } } },
      },
      { provide: Router, useValue: router },
      { provide: InscripcionProposalFacade, useValue: proposal },
      { provide: InscripcionSurveyFacade, useValue: survey },
      { provide: InscripcionPaymentFacade, useValue: payment },
    ],
  });

  const process = TestBed.inject(InscripcionProcessStore);
  if (options.spyGoTo) vi.spyOn(process.flow, 'goTo');
  const facade = TestBed.inject(InscripcionProcessFacade);

  return { facade, payment, process, proposal, survey, router };
}

function createInitialSurvey(
  values: Partial<InscripcionInitialSurvey> = {}
): InscripcionInitialSurvey {
  return {
    carreraId: null,
    comienzoId: null,
    turnoId: null,
    nivelProductoId: null,
    completa: false,
    seccionActiva: null,
    cursaSecundaria: null,
    orientacionBachilleratoId: null,
    anioBachilleratoId: null,
    recursaAnioBachillerato: null,
    vecesRecursaAnioBachillerato: null,
    institucionSecundariaId: null,
    ubicacionSecundariaId: null,
    nombreInstitucionSecundaria: null,
    estadoEducacionSuperiorPreviaId: null,
    nivelFormacionMadreId: null,
    nivelFormacionPadreId: null,
    madreEgresadaOrt: null,
    padreEgresadoOrt: null,
    anioDecisionCarreraId: null,
    anioDecisionOrtId: null,
    seInformoEnOtrasUniversidades: null,
    apoyoDecisionId: null,
    nivelDecisionId: null,
    tuvoAsesoramientoOrt: null,
    valoracionAsesoramientoOrt: null,
    visitoSitioWebOrt: null,
    valoracionSitioWebOrt: null,
    visitoInstalacionesOrt: null,
    valoracionInstalacionesOrt: null,
    recuerdaPublicidadOrt: null,
    ...values,
  };
}

function createPendingPaymentDetail(): InscripcionDetail {
  return {
    estado: 'Pago pendiente',
    detalle: null,
    intereses: [],
    pagoPendiente: {
      idInscripcion: 1072704,
      senia: 15500,
      saldoCuenta: 1200,
      fechaVencimientoPago: '2027-03-04',
      resumen: {
        idOferta: 300,
        idProducto: 20,
        carrera: 'Sistemas',
        comienzo: 'Marzo 2027',
        turno: 'Noche',
      },
      seminarios: [],
    },
    seniaMinima: null,
    confirmada: null,
  };
}

function createConfirmedDetail(): InscripcionDetail {
  return {
    estado: 'Confirmada',
    detalle: null,
    intereses: [],
    pagoPendiente: null,
    seniaMinima: null,
    confirmada: {
      numeroEstudiante: 397654,
      resumen: {
        idOferta: 300,
        idProducto: 20,
        carrera: 'Sistemas',
        comienzo: 'Marzo 2027',
        turno: 'Noche',
      },
      coordinadorAcademico: null,
      coordinadorCursos: null,
      inscripciones: [],
    },
  };
}

function createSeniaMinimaDetail(): InscripcionDetail {
  return {
    estado: 'Pago pendiente',
    detalle: null,
    intereses: [],
    pagoPendiente: null,
    seniaMinima: { metodoPago: 'ABITAB', cedula: '12345678', codigoPersona: 555, senia: 3339 },
    confirmada: null,
  };
}
