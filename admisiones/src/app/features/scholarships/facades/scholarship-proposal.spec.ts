import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ScholarshipAcademicData } from '../services/scholarship-academic-data';
import { ScholarshipProcessFacade } from './scholarship-process';
import { ScholarshipProposalFacade } from './scholarship-proposal';

describe('ScholarshipProposalFacade', () => {
  function setup(data: { carrera: string; comienzo: string; turno: string }[] = []): {
    facade: ScholarshipProposalFacade;
    process: ScholarshipProcessFacade;
  } {
    TestBed.configureTestingModule({
      providers: [
        ScholarshipProcessFacade,
        ScholarshipProposalFacade,
        {
          provide: ScholarshipAcademicData,
          useValue: { getAcademicStepData: vi.fn().mockReturnValue(of(data)) },
        },
      ],
    });

    return {
      facade: TestBed.inject(ScholarshipProposalFacade),
      process: TestBed.inject(ScholarshipProcessFacade),
    };
  }

  it('preselects the only inscription available', () => {
    const { facade } = setup([{ carrera: 'Ingeniería', comienzo: 'Marzo', turno: 'Noche' }]);

    expect(facade.selectedInscription()).toBe('Ingeniería');
    expect(facade.selectedAcademicStepData().comienzo).toBe('Marzo');
  });

  it('does not advance the process with an incomplete section', () => {
    const { facade, process } = setup([
      { carrera: 'Ingeniería', comienzo: 'Marzo', turno: 'Noche' },
      { carrera: 'Diseño', comienzo: 'Agosto', turno: 'Mañana' },
    ]);

    facade.continue();

    expect(facade.showErrorAlert()).toBe(true);
    expect(process.currentStep()).toBe('application-info');
  });

  it('advances the process once the section is complete', () => {
    const { facade, process } = setup([
      { carrera: 'Ingeniería', comienzo: 'Marzo', turno: 'Noche' },
      { carrera: 'Diseño', comienzo: 'Agosto', turno: 'Mañana' },
    ]);

    facade.onInscriptionSelectionChange('Diseño');
    facade.continue();

    expect(facade.showErrorAlert()).toBe(false);
    expect(process.currentStep()).toBe('personal-info');
  });
});
