import { TestBed } from '@angular/core/testing';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { ScholarshipProposalFacade } from './scholarship-proposal';

describe('ScholarshipProposalFacade', () => {
  function setup(): ScholarshipProposalFacade {
    TestBed.configureTestingModule({
      providers: [ScholarshipProposalFacade, { provide: AcademicProposalSelection, useValue: {} }],
    });
    return TestBed.inject(ScholarshipProposalFacade);
  }

  it('blocks continuing with an incomplete proposal', () => {
    const facade = setup();

    expect(facade.canContinue()).toBe(false);
    expect(facade.submitted()).toBe(true);
    expect(facade.academicForm.touched).toBe(true);
  });

  it('allows continuing with a complete proposal', () => {
    const facade = setup();

    facade.academicForm.setValue({
      proposalType: '1',
      degreeProgram: '20',
      intake: '200',
      shift: '300',
      seminars: [],
    });

    expect(facade.canContinue()).toBe(true);
  });
});
