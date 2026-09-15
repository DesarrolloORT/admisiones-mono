import { TestBed } from '@angular/core/testing';

import { EnrollmentSurveyFacade } from '../../../facades/enrollment-survey';
import { EnrollmentRegulationReader } from './enrollment-regulation-reader';

describe('EnrollmentRegulationReader', () => {
  it('creates with its step facade', () => {
    TestBed.configureTestingModule({
      imports: [EnrollmentRegulationReader],
      providers: [{ provide: EnrollmentSurveyFacade, useValue: {} }],
    }).overrideComponent(EnrollmentRegulationReader, { set: { imports: [], template: '' } });

    expect(TestBed.createComponent(EnrollmentRegulationReader).componentInstance).toBeTruthy();
  });
});
