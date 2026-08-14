import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import { Inscripciones } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionProposalFacade } from './inscription-proposal';

describe('InscripcionProposalFacade', () => {
  const registerProductInterest = vi.fn();
  let facade: InscripcionProposalFacade;
  let process: InscripcionProcessStore;

  beforeEach(() => {
    registerProductInterest.mockReset().mockReturnValue(of(true));
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        InscripcionFormsStore,
        InscripcionProcessStore,
        InscripcionProposalFacade,
        {
          provide: Catalogs,
          useValue: {
            getDegreePrograms: () =>
              of([
                {
                  productId: 20,
                  productLevelId: 1,
                  productName: 'Ingeniería en Sistemas',
                  productLevelName: 'Carrera universitaria',
                },
                {
                  productId: 21,
                  productLevelId: 3,
                  admissionProcessId: 200,
                  productName: 'Programa de Asesoramiento Financiero',
                  productLevelName: 'Actualización profesional',
                  hasSeminar: true,
                },
              ]),
            getIntakes: () =>
              of([{ admissionProcessId: 200, admissionProcessName: 'Agosto 2026' }]),
            getShifts: () =>
              of([
                {
                  offeringId: 300,
                  shiftId: 10,
                  shiftName: 'Nocturno',
                  referenceSchedule: '19:00 a 23:00',
                },
              ]),
            getSeminars: () =>
              of([
                { offeringId: 300, admissionProcessId: 200, name: 'Marco legal', startDate: null },
                { offeringId: 301, admissionProcessId: 200, name: 'Renta fija', startDate: null },
              ]),
          },
        },
        { provide: Inscripciones, useValue: { registerProductInterest } },
      ],
    });
    facade = TestBed.inject(InscripcionProposalFacade);
    process = TestBed.inject(InscripcionProcessStore);
  });

  it('registers the proposal before advancing', () => {
    setValidProposal(facade);

    facade.continue();

    expect(registerProductInterest).toHaveBeenCalledWith({
      idOfertas: [300],
      idProcesoSeleccionado: 200,
      idProducto: 20,
    });
    expect(process.flow.currentStep()).toBe('encuesta');
  });

  it('registers all selected seminars for a professional update proposal', () => {
    facade.academicForm.controls.proposalType.setValue('3');
    facade.academicForm.controls.degreeProgram.setValue('21');
    facade.academicForm.controls.seminars.setValue(['300', '301']);

    facade.continue();

    expect(registerProductInterest).toHaveBeenCalledWith({
      idOfertas: [300, 301],
      idProcesoSeleccionado: 200,
      idProducto: 21,
    });
    expect(process.flow.currentStep()).toBe('encuesta');
  });

  it('requires at least one seminar with AP terminology in the error summary', () => {
    facade.academicForm.controls.proposalType.setValue('3');
    facade.academicForm.controls.degreeProgram.setValue('21');

    facade.continue();

    expect(registerProductInterest).not.toHaveBeenCalled();
    expect(process.flow.currentStep()).toBe('propuesta');
    expect(
      facade
        .academicErrors()
        .map(error => error.message)
        .join(' ')
    ).toContain('Seminario');
  });

  it('stays on the proposal when registration fails', () => {
    registerProductInterest.mockReturnValue(of(false));
    setValidProposal(facade);

    facade.continue();

    expect(process.flow.currentStep()).toBe('propuesta');
    expect(facade.academicErrors()[0]?.message).toContain('No se pudo registrar');
  });

  it('keeps the resumed proposal disabled and skips product interest registration', () => {
    setValidProposal(facade);
    facade.disableForResume();

    expect(facade.academicForm.disabled).toBe(true);
    expect(facade.academicForm.controls.degreeProgram.value).toBe('20');
    expect(facade.academicForm.controls.intake.value).toBe('200');
    expect(facade.academicForm.controls.shift.value).toBe('300');

    facade.continue();

    expect(registerProductInterest).not.toHaveBeenCalled();
    expect(process.flow.currentStep()).toBe('encuesta');
  });
});

function setValidProposal(facade: InscripcionProposalFacade): void {
  facade.academicForm.controls.proposalType.setValue('1');
  facade.academicForm.controls.degreeProgram.setValue('20');
  facade.academicForm.controls.intake.setValue('200');
  facade.academicForm.controls.shift.setValue('300');
}
