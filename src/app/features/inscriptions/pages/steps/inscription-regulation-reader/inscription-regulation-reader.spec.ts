import { TestBed } from '@angular/core/testing';

import { InscripcionSurveyFacade } from '../../../facades/inscription-survey';
import { InscripcionRegulationReader } from './inscription-regulation-reader';

describe('InscripcionRegulationReader', () => {
  it('creates with its step facade', () => {
    TestBed.configureTestingModule({
      imports: [InscripcionRegulationReader],
      providers: [{ provide: InscripcionSurveyFacade, useValue: {} }],
    }).overrideComponent(InscripcionRegulationReader, { set: { imports: [], template: '' } });

    expect(TestBed.createComponent(InscripcionRegulationReader).componentInstance).toBeTruthy();
  });
});
