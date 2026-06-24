import { TestBed } from '@angular/core/testing';

import { InscripcionProposalFacade } from '../../facades/inscripcion-proposal';
import { InscripcionAcademicStep } from './inscripcion-academic-step';

describe('InscripcionAcademicStep', () => {
  it('creates with its step facade', () => {
    TestBed.configureTestingModule({
      imports: [InscripcionAcademicStep],
      providers: [{ provide: InscripcionProposalFacade, useValue: {} }],
    }).overrideComponent(InscripcionAcademicStep, { set: { imports: [], template: '' } });

    expect(TestBed.createComponent(InscripcionAcademicStep).componentInstance).toBeTruthy();
  });
});
