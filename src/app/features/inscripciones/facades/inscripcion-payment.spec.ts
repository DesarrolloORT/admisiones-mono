import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import { Inscripciones } from '../services/inscripciones';
import { InscripcionFormsStore } from '../store/inscripcion-forms';
import { InscripcionProcessStore } from '../store/inscripcion-process';
import { InscripcionPaymentFacade } from './inscripcion-payment';
import { InscripcionProposalFacade } from './inscripcion-proposal';

describe('InscripcionPaymentFacade', () => {
  let facade: InscripcionPaymentFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        InscripcionFormsStore,
        InscripcionProcessStore,
        InscripcionProposalFacade,
        InscripcionPaymentFacade,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({}) } },
        },
        {
          provide: Catalogs,
          useValue: {
            getCareers: () => of([]),
            getComienzos: () => of([]),
            getTurnos: () => of([]),
          },
        },
        { provide: Inscripciones, useValue: {} },
      ],
    });
    facade = TestBed.inject(InscripcionPaymentFacade);
  });

  it('uses a confirmation subview without changing the process step', () => {
    facade.paymentForm.controls.metodoPago.setValue('paganza');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('reserva');
  });

  it('shows processing before completing an immediate payment', fakeAsync(() => {
    facade.paymentForm.controls.metodoPago.setValue('cuenta-bancaria');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.view()).toBe('processing');
    tick(1000);
    expect(facade.outcome()).toBe('inscripcion-confirmada');
  }));
});
