import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { CatalogsApi } from '../api/catalogs.api';
import type { AcademicProposalForm } from '../models/academic-proposal';
import { AcademicProposalSelection } from './academic-proposal-selection';

describe('AcademicProposalSelection', () => {
  const getDegreePrograms = vi.fn();
  const getIntakes = vi.fn();
  const getShifts = vi.fn();
  const getSeminars = vi.fn();
  let form: FormGroup<AcademicProposalForm>;
  let selection: AcademicProposalSelection;

  beforeEach(() => {
    getDegreePrograms.mockReset().mockReturnValue(
      of([
        {
          productId: 10,
          productLevelId: 1,
          productName: 'Ingeniería',
          productLevelName: 'Carrera universitaria',
        },
        {
          productId: 20,
          productLevelId: 2,
          productName: 'Analista Programador',
          productLevelName: 'Tecnicatura',
          schoolName: 'Facultad de Ingeniería',
        },
        {
          admissionProcessId: 200,
          productId: 30,
          productLevelId: 3,
          productName: 'Programa de Asesoramiento Financiero',
          productLevelName: 'Actualización profesional',
        },
      ])
    );
    getIntakes
      .mockReset()
      .mockReturnValue(of([{ admissionProcessId: 200, admissionProcessName: 'Agosto' }]));
    getShifts.mockReset().mockReturnValue(
      of([
        {
          offeringId: 300,
          shiftId: 30,
          shiftName: 'Nocturno',
          referenceSchedule: '19:00 a 23:00',
        },
      ])
    );
    getSeminars.mockReset().mockReturnValue(
      of([
        { offeringId: 300, admissionProcessId: 200, name: 'Marco legal', startDate: '19/05/2026' },
        { offeringId: 301, admissionProcessId: 201, name: 'Renta fija', startDate: null },
      ])
    );
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        {
          provide: CatalogsApi,
          useValue: {
            getDegreePrograms,
            getIntakes,
            getShifts,
            getSeminars,
          },
        },
      ],
    });
    form = createForm();
    selection = TestBed.inject(AcademicProposalSelection);
    selection.connect(form);
  });

  it('loads options following the proposal, degreeProgram and start cascade', () => {
    expect(selection.proposalOptions().map(option => option.value)).toEqual(['1', '2', '3']);
    expect(getDegreePrograms).not.toHaveBeenCalled();

    form.controls.proposalType.setValue('2');
    expect(getDegreePrograms).toHaveBeenCalledWith(2);
    expect(selection.degreeProgramOptions()).toEqual([
      { value: '20', label: 'Analista Programador', school: 'Facultad de Ingeniería' },
    ]);

    form.controls.degreeProgram.setValue('20');
    expect(getIntakes).toHaveBeenCalledWith(20);
    expect(selection.intakeOptions()).toEqual([{ value: '200', label: 'Agosto' }]);

    form.controls.intake.setValue('200');
    expect(getShifts).toHaveBeenCalledWith(20, 200);
    expect(selection.shiftOptions()[0]?.value).toBe('300');
  });

  it('clears dependent controls when the proposal changes', () => {
    form.setValue({
      proposalType: '1',
      degreeProgram: '10',
      intake: '200',
      shift: '300',
      seminars: [],
    });

    form.controls.proposalType.setValue('2');

    expect(form.getRawValue()).toEqual({
      proposalType: '2',
      degreeProgram: '',
      intake: '',
      shift: '',
      seminars: [],
    });
  });

  it('exposes the AP terminology only for the professional update proposal', () => {
    expect(selection.isProfessionalUpdate()).toBe(false);
    expect(selection.terminology().degreeProgramLabel).toBe('Carrera');

    form.controls.proposalType.setValue('3');

    expect(selection.isProfessionalUpdate()).toBe(true);
    expect(selection.terminology().degreeProgramLabel).toBe('Programa');
    expect(selection.terminology().intakeLabel).toBe('Seminario');
  });

  it('loads seminars instead of starts when an AP program is selected', () => {
    form.controls.proposalType.setValue('3');
    form.controls.degreeProgram.setValue('30');

    expect(getSeminars).toHaveBeenCalledWith(30, 200);
    expect(getIntakes).not.toHaveBeenCalled();
    expect(selection.seminarOptions()).toEqual([
      { value: '300', label: 'Marco legal', description: '19/05/2026' },
      { value: '301', label: 'Renta fija', description: undefined },
    ]);
    expect(selection.canSelectSeminars()).toBe(true);
  });

  it('loads seminars when an AP program was prefilled before catalogs arrived', () => {
    const resumedForm = createForm();
    resumedForm.setValue({
      proposalType: '3',
      degreeProgram: '30',
      intake: '200',
      shift: '300',
      seminars: ['300', '301'],
    });
    getSeminars.mockClear();

    selection.connect(resumedForm);

    expect(getSeminars).toHaveBeenCalledWith(30, 200);
    expect(selection.seminarOptions()).toHaveLength(2);
  });

  it('clears the selected seminars when the program changes', () => {
    form.controls.proposalType.setValue('3');
    form.controls.degreeProgram.setValue('30');
    form.controls.seminars.setValue(['300']);

    form.controls.degreeProgram.setValue('');

    expect(form.controls.seminars.value).toEqual([]);
    expect(selection.seminarOptions()).toEqual([]);
  });

  it('swaps required validators between start/shift and seminars per proposal type', () => {
    form.controls.proposalType.setValue('3');

    expect(form.controls.intake.hasValidator).toBeDefined();
    expect(form.controls.intake.valid).toBe(true);
    expect(form.controls.shift.valid).toBe(true);
    expect(form.controls.seminars.hasError('required')).toBe(true);

    form.controls.proposalType.setValue('1');

    expect(form.controls.intake.hasError('required')).toBe(true);
    expect(form.controls.shift.hasError('required')).toBe(true);
    expect(form.controls.seminars.hasError('required')).toBe(false);
  });

  // Sin `hasSeminar` el programa ofrece una sola oferta y el select es simple.
  it('names the AP offering field after the seminars flag of the program', () => {
    form.controls.proposalType.setValue('3');
    form.controls.degreeProgram.setValue('30');

    expect(selection.allowsMultipleSeminars()).toBe(false);
    expect(selection.seminarLabel()).toBe('Próximo comienzo');
    expect(selection.seminarErrorText()).toBe('Seleccioná un próximo comienzo');

    getDegreePrograms.mockReturnValue(
      of([
        {
          admissionProcessId: 200,
          productId: 30,
          productLevelId: 3,
          productName: 'Programa de Asesoramiento Financiero',
          productLevelName: 'Actualización profesional',
          hasSeminar: true,
        },
      ])
    );
    selection.setProposalType('3');
    form.controls.degreeProgram.setValue('30');

    expect(selection.seminarLabel()).toBe('Seminario');
    expect(selection.seminarErrorText()).toBe('Seleccioná al menos un seminario');
  });

  it('preselects the only option of each catalog without touching an existing selection', () => {
    form.controls.proposalType.setValue('1');
    form.controls.degreeProgram.setValue('10');
    TestBed.tick();

    // Un solo comienzo se precarga y encadena el turno, que también viene solo.
    expect(form.controls.intake.value).toBe('200');
    expect(form.controls.shift.value).toBe('300');

    getIntakes.mockReturnValue(
      of([
        { admissionProcessId: 200, admissionProcessName: 'Agosto' },
        { admissionProcessId: 201, admissionProcessName: 'Marzo' },
      ])
    );
    form.controls.degreeProgram.setValue('20');
    TestBed.tick();

    expect(form.controls.intake.value).toBe('');
  });

  it('preselects a single seminar but keeps the seminars restored on resume', () => {
    getSeminars.mockReturnValue(
      of([{ offeringId: 300, admissionProcessId: 200, name: 'Marco legal', startDate: null }])
    );
    form.controls.proposalType.setValue('3');
    form.controls.degreeProgram.setValue('30');
    TestBed.tick();

    expect(form.controls.seminars.value).toEqual(['300']);

    const resumedForm = createForm();
    resumedForm.setValue({
      proposalType: '3',
      degreeProgram: '30',
      intake: '',
      shift: '',
      seminars: ['301'],
    });
    selection.connect(resumedForm);
    TestBed.tick();

    expect(resumedForm.controls.seminars.value).toEqual(['301']);
  });
});

function createForm(): FormGroup<AcademicProposalForm> {
  return new FormGroup<AcademicProposalForm>({
    proposalType: new FormControl('', { nonNullable: true }),
    degreeProgram: new FormControl('', { nonNullable: true }),
    intake: new FormControl('', { nonNullable: true }),
    shift: new FormControl('', { nonNullable: true }),
    seminars: new FormControl<string[]>([], { nonNullable: true }),
  });
}
