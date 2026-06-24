import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { of } from 'rxjs';

import type { AcademicProposalForm } from '../../models/academic-proposal';
import { Catalogs } from '../../services/catalogs';
import { AcademicProposalSelect } from './academic-proposal-select';

describe('AcademicProposalSelect', () => {
  let fixture: ComponentFixture<AcademicProposalSelect>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [AcademicProposalSelect],
      providers: [
        {
          provide: Catalogs,
          useValue: {
            getCareers: () => of([]),
            getComienzos: () => of([]),
            getTurnos: () => of([]),
          },
        },
      ],
    });
    fixture = TestBed.createComponent(AcademicProposalSelect);
    fixture.componentRef.setInput('form', createForm());
    await fixture.whenStable();
  });

  it('connects a form to its academic selection state', () => {
    expect(fixture.componentInstance.selection().initialized()).toBe(true);
  });
});

function createForm(): FormGroup<AcademicProposalForm> {
  return new FormGroup<AcademicProposalForm>({
    tipoPropuesta: new FormControl('', { nonNullable: true }),
    carrera: new FormControl('', { nonNullable: true }),
    comienzo: new FormControl('', { nonNullable: true }),
    turno: new FormControl('', { nonNullable: true }),
  });
}
