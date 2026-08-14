import { TestBed } from '@angular/core/testing';

import { EnrollmentPaymentFacade } from '../../../facades/enrollment-payment';
import { EnrollmentReservationStep } from './enrollment-reservation-step';

describe('EnrollmentReservationStep', () => {
  it('creates with its step facade', () => {
    TestBed.configureTestingModule({
      imports: [EnrollmentReservationStep],
      providers: [{ provide: EnrollmentPaymentFacade, useValue: {} }],
    }).overrideComponent(EnrollmentReservationStep, { set: { imports: [], template: '' } });

    expect(TestBed.createComponent(EnrollmentReservationStep).componentInstance).toBeTruthy();
  });
});
