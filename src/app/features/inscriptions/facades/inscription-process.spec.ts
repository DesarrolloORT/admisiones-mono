import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { vi } from 'vitest';

import type { InscripcionDetail } from '../models/inscription-detail';
import { InscripcionDraft } from '../services/inscription-draft';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProcessFacade } from './inscription-process';
import { InscripcionProposalFacade } from './inscription-proposal';
import { InscripcionSurveyFacade } from './inscription-survey';

describe('InscripcionProcessFacade', () => {
  it('starts clean without a resolved detail and ignores stale session drafts', () => {
    const { draft, process, payment } = createFacade(null);

    TestBed.tick();

    expect(draft.load).not.toHaveBeenCalled();
    expect(draft.clear).toHaveBeenCalledWith('primera-vez');
    expect(draft.clear).toHaveBeenCalledWith('parcial');
    expect(draft.clear).toHaveBeenCalledWith('encuesta-completa');
    expect(process.flow.currentStep()).toBe('propuesta');
    expect(process.preEnrollmentResponse()).toBeNull();
    expect(payment.outcome()).toBeNull();
  });

  it('uses pending-payment detail instead of any local draft', () => {
    const { draft, process } = createFacade(createPendingPaymentDetail());

    TestBed.tick();

    expect(draft.load).not.toHaveBeenCalled();
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

  it('uses pending-payment detail before catalogs finish initializing', () => {
    const { draft, process } = createFacade(createPendingPaymentDetail(), false);

    expect(draft.clear).not.toHaveBeenCalled();
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

  it('uses confirmed detail instead of any local draft', () => {
    const { draft, payment, process } = createFacade(createConfirmedDetail());

    TestBed.tick();

    expect(draft.load).not.toHaveBeenCalled();
    expect(process.preEnrollmentResponse()).toEqual({
      idInscripcion: null,
      confirmada: true,
      fechaVencimientoPago: null,
      seniaInscripcion: null,
      saldoCuenta: null,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
    });
    expect(payment.outcome()).toBe('inscription-confirmada');
  });
});

function createFacade(detail: InscripcionDetail | null, initialized = true) {
  const draft = {
    load: vi.fn(),
    save: vi.fn(),
    clear: vi.fn(),
  };
  const payment = {
    outcome: signal(null),
    view: signal('editing'),
    requestConfirmation: vi.fn(),
    restore: vi.fn(),
  };

  TestBed.configureTestingModule({
    providers: [
      InscripcionProcessFacade,
      InscripcionFormsStore,
      InscripcionProcessStore,
      { provide: ActivatedRoute, useValue: { snapshot: { data: { inscriptionDetail: detail } } } },
      { provide: Router, useValue: { navigateByUrl: vi.fn() } },
      { provide: InscripcionDraft, useValue: draft },
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
          completedSectionIds: vi.fn(() => []),
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

  return { draft, facade, payment, process };
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
