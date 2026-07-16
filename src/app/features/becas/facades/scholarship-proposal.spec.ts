import { TestBed } from '@angular/core/testing';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { ScholarshipFormsStore } from '../store/scholarship-forms';
import { ScholarshipProcessStore } from '../store/scholarship-process';
import { ScholarshipProposalFacade } from './scholarship-proposal';

describe('ScholarshipProposalFacade', () => {
  it('advances only with a valid proposal', () => {
    TestBed.configureTestingModule({
      providers: [
        ScholarshipFormsStore,
        ScholarshipProcessStore,
        ScholarshipProposalFacade,
        { provide: AcademicProposalSelection, useValue: {} },
      ],
    });
    const facade = TestBed.inject(ScholarshipProposalFacade);

    facade.continue();
    expect(facade.submitted()).toBe(true);
    expect(TestBed.inject(ScholarshipProcessStore).flow.currentStep()).toBe('info-postulacion');

    facade.academicForm.setValue({
      tipoPropuesta: '1',
      carrera: '20',
      comienzo: '200',
      turno: '300',
    });
    facade.continue();

    expect(TestBed.inject(ScholarshipProcessStore).flow.currentStep()).toBe('info-personal');
  });
});
