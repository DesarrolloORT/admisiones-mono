import { TestBed } from '@angular/core/testing';

import { EnrollmentPaymentFacade } from '../../../facades/enrollment-payment';
import { EnrollmentSuccessStep } from './enrollment-success-step';

describe('EnrollmentSuccessStep', () => {
  it('creates with its step facade', () => {
    TestBed.configureTestingModule({
      imports: [EnrollmentSuccessStep],
      providers: [{ provide: EnrollmentPaymentFacade, useValue: {} }],
    }).overrideComponent(EnrollmentSuccessStep, { set: { imports: [], template: '' } });

    expect(TestBed.createComponent(EnrollmentSuccessStep).componentInstance).toBeTruthy();
  });
});
