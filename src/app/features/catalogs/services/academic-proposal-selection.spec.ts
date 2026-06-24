import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { of } from 'rxjs';
import { vi } from 'vitest';

import type { AcademicProposalForm } from '../models/academic-proposal';
import { AcademicProposalSelection } from './academic-proposal-selection';
import { Catalogs } from './catalogs';

describe('AcademicProposalSelection', () => {
  const getComienzos = vi.fn();
  const getTurnos = vi.fn();
  let form: FormGroup<AcademicProposalForm>;
  let selection: AcademicProposalSelection;

  beforeEach(() => {
    getComienzos.mockReset().mockReturnValue(of([{ idProceso: 200, nombreProceso: 'Agosto' }]));
    getTurnos.mockReset().mockReturnValue(
      of([
        {
          idOferta: 300,
          idTurno: 30,
          nombreTurno: 'Nocturno',
          horarioReferencia: '19:00 a 23:00',
        },
      ])
    );
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        {
          provide: Catalogs,
          useValue: {
            getCareers: () =>
              of([
                {
                  idProducto: 10,
                  idNivelProducto: 1,
                  nombreProducto: 'Ingeniería',
                  nombreNivelProducto: 'Carrera universitaria',
                },
                {
                  idProducto: 20,
                  idNivelProducto: 2,
                  nombreProducto: 'Analista Programador',
                  nombreNivelProducto: 'Tecnicatura',
                },
              ]),
            getComienzos,
            getTurnos,
          },
        },
      ],
    });
    form = createForm();
    selection = TestBed.inject(AcademicProposalSelection);
    selection.connect(form);
  });

  it('loads options following the proposal, career and start cascade', () => {
    form.controls.tipoPropuesta.setValue('2');
    expect(selection.careerOptions()).toEqual([{ value: '20', label: 'Analista Programador' }]);

    form.controls.carrera.setValue('20');
    expect(getComienzos).toHaveBeenCalledWith(20);
    expect(selection.startOptions()).toEqual([{ value: '200', label: 'Agosto' }]);

    form.controls.comienzo.setValue('200');
    expect(getTurnos).toHaveBeenCalledWith(20, 200);
    expect(selection.shiftOptions()[0]?.value).toBe('300');
  });

  it('clears dependent controls when the proposal changes', () => {
    form.setValue({ tipoPropuesta: '1', carrera: '10', comienzo: '200', turno: '300' });

    form.controls.tipoPropuesta.setValue('2');

    expect(form.getRawValue()).toEqual({
      tipoPropuesta: '2',
      carrera: '',
      comienzo: '',
      turno: '',
    });
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
