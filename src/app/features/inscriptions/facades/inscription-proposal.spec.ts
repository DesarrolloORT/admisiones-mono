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
            getCareers: () =>
              of([
                {
                  idProducto: 20,
                  idNivelProducto: 1,
                  nombreProducto: 'Ingeniería en Sistemas',
                  nombreNivelProducto: 'Carrera universitaria',
                },
                {
                  idProducto: 21,
                  idNivelProducto: 3,
                  idProceso: 200,
                  nombreProducto: 'Programa de Asesoramiento Financiero',
                  nombreNivelProducto: 'Actualización profesional',
                  tieneSeminario: true,
                },
              ]),
            getComienzos: () => of([{ idProceso: 200, nombreProceso: 'Agosto 2026' }]),
            getTurnos: () =>
              of([
                {
                  idOferta: 300,
                  idTurno: 10,
                  nombreTurno: 'Nocturno',
                  horarioReferencia: '19:00 a 23:00',
                },
              ]),
            getSeminarios: () =>
              of([
                { idOferta: 300, idProceso: 200, nombre: 'Marco legal', fechaComienzo: null },
                { idOferta: 301, idProceso: 200, nombre: 'Renta fija', fechaComienzo: null },
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
    facade.academicForm.controls.tipoPropuesta.setValue('3');
    facade.academicForm.controls.carrera.setValue('21');
    facade.academicForm.controls.seminarios.setValue(['300', '301']);

    facade.continue();

    expect(registerProductInterest).toHaveBeenCalledWith({
      idOfertas: [300, 301],
      idProcesoSeleccionado: 200,
      idProducto: 21,
    });
    expect(process.flow.currentStep()).toBe('encuesta');
  });

  it('requires at least one seminar with AP terminology in the error summary', () => {
    facade.academicForm.controls.tipoPropuesta.setValue('3');
    facade.academicForm.controls.carrera.setValue('21');

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
    expect(facade.academicForm.controls.carrera.value).toBe('20');
    expect(facade.academicForm.controls.comienzo.value).toBe('200');
    expect(facade.academicForm.controls.turno.value).toBe('300');

    facade.continue();

    expect(registerProductInterest).not.toHaveBeenCalled();
    expect(process.flow.currentStep()).toBe('encuesta');
  });
});

function setValidProposal(facade: InscripcionProposalFacade): void {
  facade.academicForm.controls.tipoPropuesta.setValue('1');
  facade.academicForm.controls.carrera.setValue('20');
  facade.academicForm.controls.comienzo.setValue('200');
  facade.academicForm.controls.turno.setValue('300');
}
