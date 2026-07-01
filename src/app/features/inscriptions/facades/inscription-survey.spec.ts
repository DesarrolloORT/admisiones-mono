import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { firstValueFrom, of } from 'rxjs';
import { vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import type { InscripcionInitialSurvey } from '../models/inscripcion-flow';
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
      encuesta: createInitialSurvey({ seccionActiva: 'decision-academica' }),
      universidadesConsideradas: [],
      universidadesEducacionSuperior: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });

    expect(process.flow.currentStep()).toBe('encuesta');
    expect(survey.activeSection()).toBe('decision-academica');
  });

  it('keeps only identity and regulation when the survey is not required', async () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesEducacionSuperior: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });

    expect(survey.visibleSections()).toEqual(['identidad', 'reglamento']);
    await expect(firstValueFrom(survey.savePartial())).resolves.toBe(true);
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  it('uses school years and only asks baccalaureate details after 1 EMS', () => {
    const { survey } = createFacade(
      {
        tieneDerechoEncuesta: true,
        encuesta: null,
        universidadesConsideradas: [],
        universidadesEducacionSuperior: [],
        opcionesMotivosSeleccionados: [],
        opcionesPublicidadSeleccionadas: [],
      },
      {
        educacion: {
          ubicacionesUltimoAnioSecundaria: [],
          estadosEducacionSuperiorPrevia: [],
          universidades: [],
          nivelesFormacionTutores: [],
          aniosBachillerato: [
            { id: 10, label: '1 EMS', baccalaureates: [] },
            {
              id: 11,
              label: '2 EMS',
              baccalaureates: [{ id: 20, label: 'Nacional', orientation: 'Cientifico' }],
            },
            {
              id: 12,
              label: '3 EMS',
              baccalaureates: [{ id: 30, label: 'Nacional', orientation: 'Economia' }],
            },
          ],
        },
      }
    );

    expect(survey.schoolYearOptions()).toEqual([
      { value: '10', label: '1 EMS' },
      { value: '11', label: '2 EMS' },
      { value: '12', label: '3 EMS' },
    ]);

    survey.educationForm.patchValue({ cursaSecundaria: 'cursando', anioSecundaria: '10' });

    expect(survey.shouldAskBaccalaureateType()).toBe(false);
    expect(survey.baccalaureateOptions()).toEqual([]);
    expect(survey.orientationOptions()).toEqual([]);

    survey.educationForm.controls.anioSecundaria.setValue('11');

    expect(survey.shouldAskBaccalaureateType()).toBe(true);
    expect(survey.baccalaureateOptions()).toEqual([{ value: '20', label: 'Nacional' }]);

    survey.educationForm.controls.tipoBachillerato.setValue('20');

    expect(survey.orientationOptions()).toEqual([{ value: '20', label: 'Cientifico' }]);

    survey.educationForm.controls.anioSecundaria.setValue('12');

    expect(survey.baccalaureateOptions()).toEqual([{ value: '30', label: 'Nacional' }]);
    expect(survey.educationForm.controls.tipoBachillerato.value).toBe('');

    survey.educationForm.controls.tipoBachillerato.setValue('30');

    expect(survey.orientationOptions()).toEqual([{ value: '30', label: 'Economia' }]);
  });

  function createFacade(
    initialSurvey: unknown,
    catalogOverrides: Record<string, unknown> = {}
  ): {
    survey: InscripcionSurveyFacade;
    process: InscripcionProcessStore;
  } {
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
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
            getCountryLocations: () => of([]),
            getInstituciones: () => of([]),
            getInitialSurveyCatalogs: () =>
              of({
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
                ...catalogOverrides,
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

  function createInitialSurvey(
    values: Partial<InscripcionInitialSurvey> = {}
  ): InscripcionInitialSurvey {
    return {
      carreraId: null,
      comienzoId: null,
      turnoId: null,
      nivelProductoId: null,
      completa: false,
      seccionActiva: null,
      cursaSecundaria: null,
      orientacionBachilleratoId: null,
      anioBachilleratoId: null,
      institucionSecundariaId: null,
      ubicacionSecundariaId: null,
      nombreInstitucionSecundaria: null,
      tieneEducacionSuperior: null,
      nivelFormacionMadreId: null,
      nivelFormacionPadreId: null,
      madreEgresadaOrt: null,
      padreEgresadoOrt: null,
      anioDecisionCarreraId: null,
      anioDecisionOrtId: null,
      seInformoEnOtrasUniversidades: null,
      apoyoPadres: null,
      apoyoOtros: null,
      apoyoAmigosFamiliares: null,
      apoyoNadie: null,
      apoyoAmigoPropuesta: null,
      decisionConfirmada: null,
      tuvoAsesoramientoOrt: null,
      valoracionAsesoramientoOrt: null,
      visitoSitioWebOrt: null,
      valoracionSitioWebOrt: null,
      visitoInstalacionesOrt: null,
      valoracionInstalacionesOrt: null,
      recuerdaPublicidadOrt: null,
      ...values,
    };
  }
});
