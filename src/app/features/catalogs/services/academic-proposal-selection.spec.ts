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
  const getSeminarios = vi.fn();
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
    getSeminarios.mockReset().mockReturnValue(
      of([
        { idOferta: 300, idProceso: 200, nombre: 'Marco legal', fechaComienzo: '19/05/2026' },
        { idOferta: 301, idProceso: 201, nombre: 'Renta fija', fechaComienzo: null },
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
                  nombreEscuela: 'Facultad de Ingeniería',
                },
                {
                  idProceso: 200,
                  idProducto: 30,
                  idNivelProducto: 3,
                  nombreProducto: 'Programa de Asesoramiento Financiero',
                  nombreNivelProducto: 'Actualización profesional',
                },
              ]),
            getComienzos,
            getTurnos,
            getSeminarios,
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
    expect(selection.careerOptions()).toEqual([
      { value: '20', label: 'Analista Programador', school: 'Facultad de Ingeniería' },
    ]);

    form.controls.carrera.setValue('20');
    expect(getComienzos).toHaveBeenCalledWith(20);
    expect(selection.startOptions()).toEqual([{ value: '200', label: 'Agosto' }]);

    form.controls.comienzo.setValue('200');
    expect(getTurnos).toHaveBeenCalledWith(20, 200);
    expect(selection.shiftOptions()[0]?.value).toBe('300');
  });

  it('clears dependent controls when the proposal changes', () => {
    form.setValue({
      tipoPropuesta: '1',
      carrera: '10',
      comienzo: '200',
      turno: '300',
      seminarios: [],
    });

    form.controls.tipoPropuesta.setValue('2');

    expect(form.getRawValue()).toEqual({
      tipoPropuesta: '2',
      carrera: '',
      comienzo: '',
      turno: '',
      seminarios: [],
    });
  });

  it('exposes the AP terminology only for the professional update proposal', () => {
    expect(selection.isProfessionalUpdate()).toBe(false);
    expect(selection.terminology().careerLabel).toBe('Carrera');

    form.controls.tipoPropuesta.setValue('3');

    expect(selection.isProfessionalUpdate()).toBe(true);
    expect(selection.terminology().careerLabel).toBe('Programa');
    expect(selection.terminology().startLabel).toBe('Seminario');
  });

  it('loads seminars instead of starts when an AP program is selected', () => {
    form.controls.tipoPropuesta.setValue('3');
    form.controls.carrera.setValue('30');

    expect(getSeminarios).toHaveBeenCalledWith(30, 200);
    expect(getComienzos).not.toHaveBeenCalled();
    expect(selection.seminarOptions()).toEqual([
      { value: '300', label: 'Marco legal', description: '19/05/2026' },
      { value: '301', label: 'Renta fija', description: undefined },
    ]);
    expect(selection.canSelectSeminars()).toBe(true);
  });

  it('loads seminars when an AP program was prefilled before catalogs arrived', () => {
    const resumedForm = createForm();
    resumedForm.setValue({
      tipoPropuesta: '3',
      carrera: '30',
      comienzo: '200',
      turno: '300',
      seminarios: ['300', '301'],
    });
    getSeminarios.mockClear();

    selection.connect(resumedForm);

    expect(getSeminarios).toHaveBeenCalledWith(30, 200);
    expect(selection.seminarOptions()).toHaveLength(2);
  });

  it('clears the selected seminars when the program changes', () => {
    form.controls.tipoPropuesta.setValue('3');
    form.controls.carrera.setValue('30');
    form.controls.seminarios.setValue(['300']);

    form.controls.carrera.setValue('');

    expect(form.controls.seminarios.value).toEqual([]);
    expect(selection.seminarOptions()).toEqual([]);
  });

  it('swaps required validators between start/shift and seminars per proposal type', () => {
    form.controls.tipoPropuesta.setValue('3');

    expect(form.controls.comienzo.hasValidator).toBeDefined();
    expect(form.controls.comienzo.valid).toBe(true);
    expect(form.controls.turno.valid).toBe(true);
    expect(form.controls.seminarios.hasError('required')).toBe(true);

    form.controls.tipoPropuesta.setValue('1');

    expect(form.controls.comienzo.hasError('required')).toBe(true);
    expect(form.controls.turno.hasError('required')).toBe(true);
    expect(form.controls.seminarios.hasError('required')).toBe(false);
  });
});

function createForm(): FormGroup<AcademicProposalForm> {
  return new FormGroup<AcademicProposalForm>({
    tipoPropuesta: new FormControl('', { nonNullable: true }),
    carrera: new FormControl('', { nonNullable: true }),
    comienzo: new FormControl('', { nonNullable: true }),
    turno: new FormControl('', { nonNullable: true }),
    seminarios: new FormControl<string[]>([], { nonNullable: true }),
  });
}
