import { TestBed } from '@angular/core/testing';

import { InscripcionPaymentFacade } from '../../facades/inscripcion-payment';
import { InscripcionReservationStep } from './inscripcion-reservation-step';

describe('InscripcionReservationStep', () => {
  it('creates with its step facade', () => {
    TestBed.configureTestingModule({
      imports: [InscripcionReservationStep],
      providers: [{ provide: InscripcionPaymentFacade, useValue: {} }],
    }).overrideComponent(InscripcionReservationStep, { set: { imports: [], template: '' } });

    expect(TestBed.createComponent(InscripcionReservationStep).componentInstance).toBeTruthy();
  });
});
