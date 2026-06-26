import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { of } from 'rxjs';

import type { AcademicProposalForm } from '../../models/academic-proposal';
import { Catalogs } from '../../services/catalogs';
import { AcademicProposalSelect } from './academic-proposal-select';

describe('AcademicProposalSelect', () => {
  let fixture: ComponentFixture<AcademicProposalSelect>;
  let form: FormGroup<AcademicProposalForm>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [AcademicProposalSelect],
      providers: [
        {
          provide: Catalogs,
          useValue: {
            getCareers: () =>
              of([
                {
                  idProducto: 20,
                  idNivelProducto: 1,
                  nombreProducto: 'Licenciatura en Diseño Gráfico',
                  nombreNivelProducto: 'Carrera universitaria',
                },
              ]),
            getComienzos: () => of([]),
            getTurnos: () => of([]),
          },
        },
      ],
    });

    form = createForm();
    fixture = TestBed.createComponent(AcademicProposalSelect);
    fixture.componentRef.setInput('form', form);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('connects a form to its academic selection state', () => {
    expect(fixture.componentInstance.selection().initialized()).toBe(true);
  });

  it('shows required errors after controls are touched', () => {
    form.markAllAsTouched();
    form.updateValueAndValidity();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Seleccioná una propuesta académica');
    expect(text).toContain('Seleccioná una carrera');
    expect(text).toContain('Seleccioná un comienzo');
    expect(text).toContain('Seleccioná un turno');
  });
});

function createForm(): FormGroup<AcademicProposalForm> {
  return new FormGroup<AcademicProposalForm>({
    tipoPropuesta: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    carrera: new FormControl('', { nonNullable: true, validators: Validators.required }),
    comienzo: new FormControl('', { nonNullable: true, validators: Validators.required }),
    turno: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });
}
