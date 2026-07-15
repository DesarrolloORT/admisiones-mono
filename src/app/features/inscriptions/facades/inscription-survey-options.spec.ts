import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import type {
  InitialSurveyCatalogs,
  LocationCountry,
} from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionSurveyOptionsFacade } from './inscription-survey-options';

describe('InscripcionSurveyOptionsFacade', () => {
  const getInitialSurveyCatalogs = vi.fn();
  const getCountryLocations = vi.fn();
  const getInstituciones = vi.fn();
  const onOptionsChanged = vi.fn();
  const onInitialCatalogsApplied = vi.fn();
  let isSurveyStepActive: WritableSignal<boolean>;

  beforeEach(() => {
    isSurveyStepActive = signal(true);
    getInitialSurveyCatalogs.mockReset().mockReturnValue(of(emptyCatalogs()));
    getCountryLocations.mockReset().mockReturnValue(of([]));
    getInstituciones.mockReset().mockReturnValue(of([]));
    onOptionsChanged.mockReset();
    onInitialCatalogsApplied.mockReset();
  });

  it('maps the initial survey catalogs into options and notifies callbacks', () => {
    getInitialSurveyCatalogs.mockReturnValue(
      of({
        ...emptyCatalogs(),
        situacionLaboral: { tiposJornada: [{ id: 1, label: 'Jornada completa' }] },
        experienciaOrt: {
          valoraciones: [{ id: '1', label: 'Malo' }],
          publicidadesOrt: [{ id: 7, label: 'Redes' }],
        },
      })
    );

    const options = createFacade();

    expect(options.workScheduleOptions()).toEqual([{ value: '1', label: 'Jornada completa' }]);
    expect(options.advertisingOptions()).toEqual([{ value: '7', label: 'Redes' }]);
    expect(options.ratingLabels()[1]).toBe('1 estrella: Malo');
    expect(options.initialized()).toBe(true);
    expect(options.loadingInitialSurveyCatalogs()).toBe(false);
    expect(options.catalogError()).toBeNull();
    expect(onInitialCatalogsApplied).toHaveBeenCalledOnce();
    expect(onOptionsChanged).toHaveBeenCalled();
  });

  it('reports the error and keeps empty options when the catalogs fail', () => {
    getInitialSurveyCatalogs.mockReturnValue(throwError(() => new Error('network error')));

    const options = createFacade();

    expect(options.catalogError()).toBe('No se pudieron cargar los catálogos de encuesta inicial.');
    expect(options.workScheduleOptions()).toEqual([]);
    expect(options.initialized()).toBe(true);
    expect(onInitialCatalogsApplied).not.toHaveBeenCalled();
  });

  it('loads Uruguay departments and the institutions of the selected department', () => {
    getCountryLocations.mockReturnValue(
      of([
        {
          codigoPais: 1,
          nombre: 'Uruguay',
          estado: [{ codigoPais: 1, codigoEstado: 5, nombre: 'Montevideo' }],
        },
        {
          codigoPais: 2,
          nombre: 'Argentina',
          estado: [{ codigoPais: 2, codigoEstado: 9, nombre: 'Buenos Aires' }],
        },
      ] satisfies LocationCountry[])
    );
    getInstituciones.mockReturnValue(of([{ id: 9, label: 'Liceo 1' }]));

    const options = createFacade();
    const educationForm = TestBed.inject(InscripcionFormsStore).educationForm;

    expect(options.departmentOptions()).toEqual([{ value: '5', label: 'Montevideo' }]);

    educationForm.controls.institucionEducativa.setValue('999');
    educationForm.controls.departamento.setValue('5');

    expect(getInstituciones).toHaveBeenCalledWith(1, 5);
    expect(options.institutionOptions()).toEqual([{ value: '9', label: 'Liceo 1' }]);
    expect(educationForm.controls.institucionEducativa.value).toBe('');
  });

  it('does not query the survey catalogs until the survey step is active', () => {
    isSurveyStepActive.set(false);

    const options = createFacade();

    expect(getInitialSurveyCatalogs).not.toHaveBeenCalled();
    expect(getCountryLocations).not.toHaveBeenCalled();

    isSurveyStepActive.set(true);
    TestBed.tick();

    expect(getInitialSurveyCatalogs).toHaveBeenCalledOnce();
    expect(getCountryLocations).toHaveBeenCalledOnce();
    expect(options.initialized()).toBe(true);
  });

  it('queries the survey catalogs only once across repeated step activations', () => {
    const options = createFacade();

    expect(getInitialSurveyCatalogs).toHaveBeenCalledOnce();

    isSurveyStepActive.set(false);
    TestBed.tick();
    isSurveyStepActive.set(true);
    TestBed.tick();

    expect(getInitialSurveyCatalogs).toHaveBeenCalledOnce();
    expect(getCountryLocations).toHaveBeenCalledOnce();
    expect(options).toBeTruthy();
  });

  function createFacade(): InscripcionSurveyOptionsFacade {
    TestBed.configureTestingModule({
      providers: [
        InscripcionFormsStore,
        InscripcionSurveyOptionsFacade,
        {
          provide: Catalogs,
          useValue: { getInitialSurveyCatalogs, getCountryLocations, getInstituciones },
        },
      ],
    });
    const options = TestBed.inject(InscripcionSurveyOptionsFacade);
    options.initialize({ isSurveyStepActive, onOptionsChanged, onInitialCatalogsApplied });
    TestBed.tick();
    return options;
  }

  function emptyCatalogs(): InitialSurveyCatalogs {
    return {
      educacion: {
        ubicacionesUltimoAnioSecundaria: [],
        aniosBachillerato: [],
        estadosEducacionSuperiorPrevia: [],
        universidades: [],
        nivelesFormacionTutores: [],
      },
      decisionAcademica: {
        aniosEducacionMediaSuperior: [],
        apoyosDecision: [],
        nivelesDecision: [],
        universidades: [],
        motivosEleccionOrt: [],
      },
      experienciaOrt: { valoraciones: [], publicidadesOrt: [] },
      situacionLaboral: { tiposJornada: [] },
    };
  }
});
