import { TestBed } from '@angular/core/testing';

import { ScholarshipProcessFacade } from './scholarship-process';
import { ScholarshipProposalFacade } from './scholarship-proposal';

describe('ScholarshipProcessFacade', () => {
  function setup(canContinue: boolean): ScholarshipProcessFacade {
    TestBed.configureTestingModule({
      providers: [
        ScholarshipProcessFacade,
        { provide: ScholarshipProposalFacade, useValue: { canContinue: () => canContinue } },
      ],
    });
    return TestBed.inject(ScholarshipProcessFacade);
  }

  it('describes and navigates the process', () => {
    const facade = setup(true);

    expect(facade.stepLabel()).toBe('Paso 1 de 3 - Información de postulación');

    facade.continue();
    expect(facade.currentStep()).toBe('personal-info');

    facade.back();
    expect(facade.currentStep()).toBe('application-info');
  });

  it('does not advance when the active section is not ready', () => {
    const facade = setup(false);

    facade.continue();

    expect(facade.currentStep()).toBe('application-info');
  });
});
