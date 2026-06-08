import '@angular/compiler';

import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../catalogs/services/catalogs';
import { InscripcionFlowFacade } from './inscripcion-flow.facade';

describe('InscripcionFlowFacade', () => {
  let facade: InscripcionFlowFacade;
  let catalogsMock: {
    getCareers: ReturnType<typeof vi.fn>;
    getComienzos: ReturnType<typeof vi.fn>;
    getInitialSurveyCatalogs: ReturnType<typeof vi.fn>;
    getTurnos: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    catalogsMock = {
      getCareers: vi.fn().mockReturnValue(
        of([
          {
            idProducto: 20,
            idNivelProducto: 1,
            nombreProducto: 'Ingeniería en Sistemas',
            nombreNivelProducto: 'Carrera universitaria',
          },
          {
            idProducto: 30,
            idNivelProducto: 2,
            nombreProducto: 'Tecnicatura en Diseño',
            nombreNivelProducto: 'Tecnicatura',
          },
        ])
      ),
      getComienzos: vi.fn().mockReturnValue(of([{ idProceso: 200, nombreProceso: 'Agosto 2026' }])),
      getInitialSurveyCatalogs: vi.fn().mockReturnValue(
        of({
          aniosAprobadosEducacionSuperior: [],
          compartidoCon: [],
          decisionCarrera: [{ id: 1, label: 'Salida laboral' }],
          decisionUniversidad: [{ id: 2, label: 'Prestigio académico' }],
          estadoEducacionSuperior: [{ id: 3, label: 'No cursé estudios superiores' }],
          formacionTutores: [{ id: 4, label: 'Universitaria completa' }],
          nivelConocimiento: [],
        })
      ),
      getTurnos: vi.fn().mockReturnValue(
        of([
          {
            idOferta: 300,
            idTurno: 10,
            nombreTurno: 'Nocturno',
            horarioReferencia: '19:00 a 23:00',
          },
        ])
      ),
    };

    TestBed.configureTestingModule({
      providers: [InscripcionFlowFacade, { provide: Catalogs, useValue: catalogsMock }],
    });

    facade = TestBed.inject(InscripcionFlowFacade);
  });

  it('should start in the academic proposal step', () => {
    expect(facade.step()).toBe('propuesta');
    expect(facade.stepNumber()).toBe(1);
    expect(facade.stepSupportLabel()).toBe('Paso 1 de 3 - Propuesta académica');
    expect(facade.canGoBack()).toBe(false);
  });

  it('should expose catalog-backed academic options', () => {
    expect(facade.proposalOptions()).toEqual([
      { value: '1', label: 'Carrera universitaria', icon: 'school' },
      { value: '2', label: 'Tecnicatura', icon: 'list_alt' },
    ]);

    facade.academicForm.controls.proposalType.setValue('1');

    expect(facade.careerOptions()).toEqual([{ value: '20', label: 'Ingeniería en Sistemas' }]);
  });

  it('should load starts and turnos from catalogs when academic selections change', () => {
    facade.academicForm.controls.career.setValue('20');

    expect(catalogsMock.getComienzos).toHaveBeenCalledWith(20);
    expect(facade.startOptions()).toEqual([{ value: '200', label: 'Agosto 2026' }]);

    facade.academicForm.controls.start.setValue('200');

    expect(catalogsMock.getTurnos).toHaveBeenCalledWith(20, 200);
    expect(facade.turnoOptions()).toEqual([{ value: '300', label: 'Nocturno (19:00 a 23:00)' }]);
  });

  it('should expose initial survey options from catalogs', () => {
    expect(facade.educationLevelOptions()).toEqual([
      { value: '4', label: 'Universitaria completa' },
    ]);
    expect(facade.academicDecisionOptions()).toEqual([{ value: '1', label: 'Salida laboral' }]);
    expect(facade.ortExperienceOptions()).toEqual([{ value: '2', label: 'Prestigio académico' }]);
    expect(facade.previousCareerOptions()).toEqual([
      { value: '3', label: 'No cursé estudios superiores' },
    ]);
  });

  it('should advance and go back through the mobile flow', () => {
    facade.continue();

    expect(facade.step()).toBe('personal');
    expect(facade.stepNumber()).toBe(2);
    expect(facade.canGoBack()).toBe(true);

    facade.continue();

    expect(facade.step()).toBe('confirmacion');
    expect(facade.stepNumber()).toBe(3);

    facade.back();

    expect(facade.step()).toBe('personal');
  });

  it('should expose the selected academic summary values', () => {
    facade.academicForm.setValue({
      proposalType: '1',
      career: '20',
      start: '200',
      turno: '300',
    });

    expect(facade.summaryItems()).toEqual([
      { icon: 'school', label: 'Carrera', value: 'Ingeniería en Sistemas' },
      { icon: 'calendar_today', label: 'Comienzo', value: 'Agosto 2026' },
      { icon: 'schedule', label: 'Turno', value: 'Nocturno (19:00 a 23:00)' },
    ]);
  });

  it('should toggle the visible subjects list', () => {
    expect(facade.visibleSubjects().length).toBe(4);
    expect(facade.subjectsToggleLabel()).toBe('Ver todas las materias');

    facade.toggleSubjects();

    expect(facade.visibleSubjects().length).toBe(6);
    expect(facade.subjectsToggleLabel()).toBe('Ver menos materias');
  });
});

