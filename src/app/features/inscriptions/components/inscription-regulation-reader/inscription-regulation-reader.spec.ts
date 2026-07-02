import { TestBed } from '@angular/core/testing';

import { InscripcionSurveyFacade } from '../../facades/inscripcion-survey';
import { InscripcionRegulationReader } from './inscripcion-regulation-reader';

describe('InscripcionRegulationReader', () => {
  it('creates with its step facade', () => {
    TestBed.configureTestingModule({
      imports: [InscripcionRegulationReader],
      providers: [{ provide: InscripcionSurveyFacade, useValue: {} }],
    }).overrideComponent(InscripcionRegulationReader, { set: { imports: [], template: '' } });

    expect(TestBed.createComponent(InscripcionRegulationReader).componentInstance).toBeTruthy();
  });
});
