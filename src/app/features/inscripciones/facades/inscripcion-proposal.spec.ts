import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import { Inscripciones } from '../services/inscripciones';
import { InscripcionFormsStore } from '../store/inscripcion-forms';
import { InscripcionProcessStore } from '../store/inscripcion-process';
import { InscripcionProposalFacade } from './inscripcion-proposal';

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
      idOferta: 300,
      idProcesoSeleccionado: 200,
      idProducto: 20,
    });
    expect(process.flow.currentStep()).toBe('encuesta');
  });

  it('stays on the proposal when registration fails', () => {
    registerProductInterest.mockReturnValue(of(false));
    setValidProposal(facade);

    facade.continue();

    expect(process.flow.currentStep()).toBe('propuesta');
    expect(facade.academicErrors()[0]?.message).toContain('No se pudo registrar');
  });
});

function setValidProposal(facade: InscripcionProposalFacade): void {
  facade.academicForm.controls.tipoPropuesta.setValue('1');
  facade.academicForm.controls.carrera.setValue('20');
  facade.academicForm.controls.comienzo.setValue('200');
  facade.academicForm.controls.turno.setValue('300');
}
