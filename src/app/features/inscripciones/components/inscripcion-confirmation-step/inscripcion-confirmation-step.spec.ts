import { TestBed } from '@angular/core/testing';

import { InscripcionPaymentFacade } from '../../facades/inscripcion-payment';
import { InscripcionConfirmationStep } from './inscripcion-confirmation-step';

describe('InscripcionConfirmationStep', () => {
  it('creates with its step facade', () => {
    TestBed.configureTestingModule({
      imports: [InscripcionConfirmationStep],
      providers: [{ provide: InscripcionPaymentFacade, useValue: {} }],
    }).overrideComponent(InscripcionConfirmationStep, { set: { imports: [], template: '' } });

    expect(TestBed.createComponent(InscripcionConfirmationStep).componentInstance).toBeTruthy();
  });
});
