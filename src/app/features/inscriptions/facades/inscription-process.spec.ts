import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import type { InscripcionDetail } from '../models/inscription-detail';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProcessFacade } from './inscription-process';
import { InscripcionProposalFacade } from './inscription-proposal';
import { InscripcionSurveyFacade } from './inscription-survey';

describe('InscripcionProcessFacade', () => {
  it('starts clean without a resolved detail', () => {
    const { process, payment } = createFacade(null);

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('propuesta');
    expect(process.preEnrollmentResponse()).toBeNull();
    expect(payment.outcome()).toBeNull();
  });

  it('resumes at the payment step with a pending-payment detail', () => {
    const { process } = createFacade(createPendingPaymentDetail());

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('pago');
    expect(process.preEnrollmentResponse()).toEqual({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: '2027-03-04',
      seniaInscripcion: 15500,
      saldoCuenta: 1200,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
    });
  });

  it('resumes at the payment step before catalogs finish initializing', () => {
    const { process } = createFacade(createPendingPaymentDetail(), false);

    expect(process.flow.currentStep()).toBe('pago');
    expect(process.preEnrollmentResponse()).toEqual({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: '2027-03-04',
      seniaInscripcion: 15500,
      saldoCuenta: 1200,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
    });
  });

  it('shows the terminal success outcome with a confirmed detail', () => {
    const { payment, process } = createFacade(createConfirmedDetail());

    TestBed.tick();

    expect(process.preEnrollmentResponse()).toEqual({
      idInscripcion: null,
      confirmada: true,
      fechaVencimientoPago: null,
      seniaInscripcion: null,
      saldoCuenta: null,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
    });
    expect(payment.outcome()).toBe('inscription-confirmada');
    expect(payment.confirmedDetail()).toEqual(createConfirmedDetail().confirmada);
  });

  it('shows the payment references when the deposit method was already chosen', () => {
    const { payment, process } = createFacade(createSeniaMinimaDetail());

    TestBed.tick();

    expect(process.flow.currentStep()).not.toBe('pago');
    expect(payment.outcome()).toBe('reserva');
    expect(payment.selectedPaymentMethod()).toBe('abitab');
    expect(process.preEnrollmentResponse()?.seniaInscripcion).toBe(3339);
  });

  it('shows the in-process outcome when the enrollment is awaiting review', () => {
    const { payment } = createFacade({
      estado: 'A la espera',
      detalle: null,
      pagoPendiente: null,
      seniaMinima: null,
      confirmada: null,
    });

    TestBed.tick();

    expect(payment.outcome()).toBe('inscription-en-proceso');
  });

  it('resumes a Pendiente detail like a pending payment', () => {
    const { payment, process } = createFacade({
      ...createPendingPaymentDetail(),
      estado: 'Pendiente',
    });

    TestBed.tick();

    expect(process.flow.currentStep()).toBe('pago');
    expect(payment.outcome()).toBeNull();
    expect(process.preEnrollmentResponse()?.idInscripcion).toBe(1072704);
  });

  it('merges the proposal and survey catalog errors', () => {
    const { facade, proposal, survey } = createFacade(null);

    expect(facade.catalogError()).toBeNull();

    survey.catalogError.set('Encuesta sin catálogos');
    expect(facade.catalogError()).toBe('Encuesta sin catálogos');

    proposal.catalogError.set('Propuesta sin catálogos');
    expect(facade.catalogError()).toBe('Propuesta sin catálogos');
  });

  it('opens and closes the exit confirmation dialog', () => {
    const { facade } = createFacade(null);

    facade.requestExit();
    expect(facade.exitConfirmationOpen()).toBe(true);

    facade.cancelExit();
    expect(facade.exitConfirmationOpen()).toBe(false);
    expect(facade.surveySaveError()).toBeNull();
  });

  it('exits without saving when the user has no initial survey right', () => {
    const { facade, survey, router } = createFacade(null);
    survey.hasInitialSurveyRight.set(false);

    facade.requestExit();
    facade.confirmExit();

    expect(survey.savePartial).not.toHaveBeenCalled();
    expect(facade.exitConfirmationOpen()).toBe(false);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/inicio');
  });

  it('saves the partial survey and navigates home on exit', () => {
    const { facade, survey, router } = createFacade(null);

    facade.requestExit();
    facade.confirmExit();

    expect(survey.savePartial).toHaveBeenCalledOnce();
    expect(facade.exitConfirmationOpen()).toBe(false);
    expect(facade.surveySaveError()).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/inicio');
  });

  it('shows an error and stays when the partial save reports failure', () => {
    const { facade, survey, router } = createFacade(null);
    survey.savePartial.mockReturnValue(of(false));

    facade.requestExit();
    facade.confirmExit();

    expect(facade.surveySaveError()).toBe('No se pudo guardar la encuesta. Intentá nuevamente.');
    expect(facade.exitConfirmationOpen()).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('shows an error and stays when the partial save fails', () => {
    const { facade, survey, router } = createFacade(null);
    survey.savePartial.mockReturnValue(throwError(() => new Error('offline')));

    facade.requestExit();
    facade.confirmExit();

    expect(facade.surveySaveError()).toBe('No se pudo guardar la encuesta. Intentá nuevamente.');
    expect(facade.exitConfirmationOpen()).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('ignores a second exit confirmation while the survey is saving', () => {
    const { facade, survey, router } = createFacade(null);
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
    const { facade, process, proposal, survey, payment } = createFacade(null);

    facade.continue();
    expect(proposal.continue).toHaveBeenCalledOnce();

    process.flow.goTo('encuesta');
    facade.continue();
    expect(survey.continue).toHaveBeenCalledOnce();

    process.flow.goTo('pago');
    facade.continue();
    expect(payment.requestConfirmation).toHaveBeenCalledOnce();
  });

  it('routes back through the survey or the flow depending on the step', () => {
    const { facade, process, survey } = createFacade(null);

    facade.back();
    expect(survey.back).not.toHaveBeenCalled();
    expect(process.flow.currentStep()).toBe('propuesta');

    process.flow.goTo('encuesta');
    facade.back();
    expect(survey.back).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('encuesta');

    process.flow.goTo('pago');
    expect(facade.canGoBack()).toBe(true);
    facade.back();
    expect(process.flow.currentStep()).toBe('encuesta');
  });

  it('blocks back while the payment is processing', () => {
    const { facade, process, payment } = createFacade(null);
    process.flow.goTo('pago');
    payment.view.set('processing');

    facade.back();

    expect(facade.canGoBack()).toBe(false);
    expect(process.flow.currentStep()).toBe('pago');
  });

  it('describes the current step and back label per state', () => {
    const { facade, process, survey } = createFacade(null);

    expect(facade.stepLabel()).toBe('Paso 1 de 3 - Propuesta académica');
    expect(facade.backLabel()).toBe('Volver al paso anterior');

    process.flow.goTo('encuesta');
    expect(facade.stepLabel()).toBe('Paso 2 de 3 - Información personal');
    expect(facade.backLabel()).toBe('Volver al paso 1');

    survey.activeSection.set('identidad');
    expect(facade.backLabel()).toBe('Volver a la sección anterior');

    survey.readerOpen.set(true);
    expect(facade.backLabel()).toBe('Volver a Reglamento estudiantil');

    process.flow.goTo('pago');
    expect(facade.stepLabel()).toBe('Paso 3 de 3 - Confirmación');
    expect(facade.backLabel()).toBe('Volver al paso 2');
  });

  it('hides the stepper on terminal outcomes and while processing', () => {
    const { facade, payment } = createFacade(null);

    expect(facade.showStepper()).toBe(true);

    payment.view.set('processing');
    expect(facade.showStepper()).toBe(false);
    expect(facade.canGoBack()).toBe(false);

    payment.view.set('editing');
    payment.outcome.set('reserva');
    expect(facade.showStepper()).toBe(false);
    expect(facade.canGoBack()).toBe(false);
  });
});

function createFacade(detail: InscripcionDetail | null, initialized = true) {
  const payment = {
    outcome: signal<string | null>(null),
    view: signal('editing'),
    confirmedDetail: signal(null),
    selectedPaymentMethod: signal(null),
    requestConfirmation: vi.fn(),
  };
  const proposal = {
    initialized: signal(initialized),
    catalogError: signal<string | null>(null),
    continue: vi.fn(),
  };
  const survey = {
    initialized: signal(initialized),
    activeSection: signal('educacion'),
    readerOpen: signal(false),
    scenario: signal('primera-vez'),
    visibleSections: signal(['educacion', 'identidad', 'reglamento']),
    catalogError: signal<string | null>(null),
    loadingSurveyState: signal(false),
    surveyLoadError: signal<string | null>(null),
    hasInitialSurveyRight: signal(true),
    back: vi.fn(),
    continue: vi.fn(),
    savePartial: vi.fn().mockReturnValue(of(true)),
  };
  const router = { navigateByUrl: vi.fn() };

  TestBed.configureTestingModule({
    providers: [
      InscripcionProcessFacade,
      InscripcionProcessStore,
      { provide: ActivatedRoute, useValue: { snapshot: { data: { inscriptionDetail: detail } } } },
      { provide: Router, useValue: router },
      { provide: InscripcionProposalFacade, useValue: proposal },
      { provide: InscripcionSurveyFacade, useValue: survey },
      { provide: InscripcionPaymentFacade, useValue: payment },
    ],
  });

  const facade = TestBed.inject(InscripcionProcessFacade);
  const process = TestBed.inject(InscripcionProcessStore);

  return { facade, payment, process, proposal, survey, router };
}

function createPendingPaymentDetail(): InscripcionDetail {
  return {
    estado: 'Pago pendiente',
    detalle: null,
    pagoPendiente: {
      idInscripcion: 1072704,
      senia: 15500,
      saldoCuenta: 1200,
      fechaVencimientoPago: '2027-03-04',
      resumen: {
        idOferta: 300,
        idProducto: 20,
        carrera: 'Sistemas',
        idComienzo: 200,
        comienzo: 'Marzo 2027',
        idTurno: 10,
        turno: 'Noche',
      },
    },
    seniaMinima: null,
    confirmada: null,
  };
}

function createConfirmedDetail(): InscripcionDetail {
  return {
    estado: 'Confirmada',
    detalle: null,
    pagoPendiente: null,
    seniaMinima: null,
    confirmada: {
      numeroEstudiante: 397654,
      resumen: {
        idOferta: 300,
        idProducto: 20,
        carrera: 'Sistemas',
        idComienzo: 200,
        comienzo: 'Marzo 2027',
        idTurno: 10,
        turno: 'Noche',
      },
      coordinadorAcademico: null,
      coordinadorCursos: null,
      materiasPrimerSemestre: [],
    },
  };
}

function createSeniaMinimaDetail(): InscripcionDetail {
  return {
    estado: 'Pago pendiente',
    detalle: null,
    pagoPendiente: null,
    seniaMinima: { metodoPago: 'ABITAB', cedula: '12345678', codigoPersona: 555, senia: 3339 },
    confirmada: null,
  };
}
