import { TestBed } from '@angular/core/testing';

import { ScholarshipProcessFacade } from './scholarship-process';

describe('ScholarshipProcessFacade', () => {
  function setup(): ScholarshipProcessFacade {
    TestBed.configureTestingModule({ providers: [ScholarshipProcessFacade] });
    return TestBed.inject(ScholarshipProcessFacade);
  }

  it('describes and navigates the process', () => {
    const facade = setup();

    expect(facade.stepLabel()).toBe('Paso 1 de 3 - Información de postulación');

    facade.continue();
    expect(facade.currentStep()).toBe('personal-info');

    facade.back();
    expect(facade.currentStep()).toBe('application-info');
  });

  it('stops at the last step', () => {
    const facade = setup();

    facade.continue();
    facade.continue();
    facade.continue();

    expect(facade.currentStep()).toBe('confirmation');
  });

  it('owns the forms so they survive step changes', () => {
    const facade = setup();

    facade.applicationForm.controls.inscription.controls.applicationMode.setValue(
      'sin declaracion'
    );
    facade.continue();

    expect(facade.applicationMode()).toBe('sin declaracion');
  });
});
