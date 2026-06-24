import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { firstValueFrom, of } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../catalogs/services/catalogs';
import { Inscripciones } from '../services/inscripciones';
import { InscripcionFormsStore } from '../store/inscripcion-forms';
import { InscripcionProcessStore } from '../store/inscripcion-process';
import { InscripcionProposalFacade } from './inscripcion-proposal';
import { InscripcionSurveyFacade } from './inscripcion-survey';

describe('InscripcionSurveyFacade', () => {
  const saveInitialSurvey = vi.fn();

  beforeEach(() => {
    saveInitialSurvey.mockReset().mockReturnValue(of(true));
  });

  it('resumes an incomplete backend survey at its active section', () => {
    const { survey, process } = createFacade({
      tieneDerechoEncuesta: true,
      encuesta: { estadoEncuestaIniAdmision: 'decision' },
      opcionesMotivosSeleccionados: null,
    });

    expect(process.flow.currentStep()).toBe('encuesta');
    expect(survey.activeSection()).toBe('decision-academica');
  });

  it('keeps only identity and regulation when the survey is not required', async () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      opcionesMotivosSeleccionados: null,
    });

    expect(survey.visibleSections()).toEqual(['identidad', 'reglamento']);
    await expect(firstValueFrom(survey.savePartial())).resolves.toBe(true);
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  function createFacade(initialSurvey: unknown): {
    survey: InscripcionSurveyFacade;
    process: InscripcionProcessStore;
  } {
    TestBed.configureTestingModule({
      providers: [
        InscripcionFormsStore,
        InscripcionProcessStore,
        InscripcionProposalFacade,
        InscripcionSurveyFacade,
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: { initialSurvey: { initialSurvey, loadFailed: false } },
              queryParamMap: convertToParamMap({}),
            },
          },
        },
        {
          provide: Catalogs,
          useValue: {
            getCareers: () => of([]),
            getComienzos: () => of([]),
            getTurnos: () => of([]),
            getInitialSurveyCatalogs: () =>
              of({
                aniosAprobadosEducacionSuperior: [],
                compartidoCon: [],
                decisionCarrera: [],
                decisionUniversidad: [],
                estadoEducacionSuperior: [],
                formacionTutores: [],
                nivelConocimiento: [],
              }),
          },
        },
        {
          provide: Inscripciones,
          useValue: {
            getStudentRegulationAcceptance: () =>
              of({ aceptoReglamentoEstudiantil: false, fechaAceptacion: null }),
            getIdentityPreload: () =>
              of({ frente: null, dorso: null, selfie: null, fechaVencimiento: null }),
            getInitialSurvey: () => of(initialSurvey),
            saveInitialSurvey,
            confirmPreEnrollment: vi.fn(),
            registerProductInterest: vi.fn(),
          },
        },
      ],
    });
    return {
      survey: TestBed.inject(InscripcionSurveyFacade),
      process: TestBed.inject(InscripcionProcessStore),
    };
  }
});
