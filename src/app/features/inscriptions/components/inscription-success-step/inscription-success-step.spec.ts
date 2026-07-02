import { TestBed } from '@angular/core/testing';

import { InscripcionPaymentFacade } from '../../facades/inscription-payment';
import { InscripcionSuccessStep } from './inscription-success-step';

describe('InscripcionSuccessStep', () => {
  it('creates with its step facade', () => {
    TestBed.configureTestingModule({
      imports: [InscripcionSuccessStep],
      providers: [{ provide: InscripcionPaymentFacade, useValue: {} }],
    }).overrideComponent(InscripcionSuccessStep, { set: { imports: [], template: '' } });

    expect(TestBed.createComponent(InscripcionSuccessStep).componentInstance).toBeTruthy();
  });
});
