import { TestBed } from '@angular/core/testing';

import { ScholarshipProcessStore } from '../store/scholarship-process';
import { ScholarshipProcessFacade } from './scholarship-process';

describe('ScholarshipProcessFacade', () => {
  it('describes and navigates the process', () => {
    TestBed.configureTestingModule({
      providers: [ScholarshipProcessFacade, ScholarshipProcessStore],
    });
    const facade = TestBed.inject(ScholarshipProcessFacade);

    expect(facade.stepLabel()).toBe('Paso 1 de 3 - Información de postulación');

    facade.continue();
    expect(facade.currentStep()).toBe('personal-info');

    facade.back();
    expect(facade.currentStep()).toBe('application-info');
  });
});
