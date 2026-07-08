import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
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
});

function createFacade(detail: InscripcionDetail | null, initialized = true) {
  const payment = {
    outcome: signal(null),
    view: signal('editing'),
    confirmedDetail: signal(null),
    requestConfirmation: vi.fn(),
  };

  TestBed.configureTestingModule({
    providers: [
      InscripcionProcessFacade,
      InscripcionProcessStore,
      { provide: ActivatedRoute, useValue: { snapshot: { data: { inscriptionDetail: detail } } } },
      { provide: Router, useValue: { navigateByUrl: vi.fn() } },
      {
        provide: InscripcionProposalFacade,
        useValue: {
          initialized: signal(initialized),
          catalogError: signal(null),
          continue: vi.fn(),
        },
      },
      {
        provide: InscripcionSurveyFacade,
        useValue: {
          initialized: signal(initialized),
          activeSection: signal('educacion'),
          readerOpen: signal(false),
          scenario: signal('primera-vez'),
          visibleSections: signal(['educacion', 'identidad', 'reglamento']),
          catalogError: signal(null),
          loadingSurveyState: signal(false),
          surveyLoadError: signal(null),
          hasInitialSurveyRight: signal(true),
          back: vi.fn(),
          continue: vi.fn(),
          savePartial: vi.fn(),
        },
      },
      { provide: InscripcionPaymentFacade, useValue: payment },
    ],
  });

  const facade = TestBed.inject(InscripcionProcessFacade);
  const process = TestBed.inject(InscripcionProcessStore);

  return { facade, payment, process };
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
    confirmada: null,
  };
}

function createConfirmedDetail(): InscripcionDetail {
  return {
    estado: 'Confirmada',
    detalle: null,
    pagoPendiente: null,
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
      materiasPrimerSemestre: [],
    },
  };
}
