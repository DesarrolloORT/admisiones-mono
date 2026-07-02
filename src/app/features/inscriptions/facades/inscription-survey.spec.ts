import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { firstValueFrom, of } from 'rxjs';
import { vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import type { InscripcionInitialSurvey } from '../models/inscription-flow';
import { Inscripciones, type InscripcionIdentityPreload } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionProposalFacade } from './inscription-proposal';
import { InscripcionSurveyFacade } from './inscription-survey';

describe('InscripcionSurveyFacade', () => {
  const saveInitialSurvey = vi.fn();
  const confirmPreEnrollment = vi.fn();
  const uploadIdentityDocument = vi.fn();
  const uploadIdentityPhoto = vi.fn();
  const getIdentityPreload = vi.fn();
  const getStudentRegulationAcceptance = vi.fn();

  beforeEach(() => {
    saveInitialSurvey.mockReset().mockReturnValue(of(true));
    confirmPreEnrollment.mockReset().mockReturnValue(
      of({
        confirmada: true,
        fechaVencimientoPago: null,
        seniaInscripcion: null,
        saldoCuenta: null,
        resumen: null,
      })
    );
    uploadIdentityDocument.mockReset().mockReturnValue(of(true));
    uploadIdentityPhoto.mockReset().mockReturnValue(of(true));
    getIdentityPreload
      .mockReset()
      .mockReturnValue(of({ frente: null, dorso: null, selfie: null, fechaVencimiento: null }));
    getStudentRegulationAcceptance
      .mockReset()
      .mockReturnValue(of({ aceptoReglamentoEstudiantil: false, fechaAceptacion: null }));
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

  it('requires identity confirmation when backend preload is complete', () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesEducacionSuperior: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    const file = preloadFile('identidad.png');

    applyIdentityPreload(survey, {
      frente: file,
      dorso: file,
      selfie: file,
      fechaVencimiento: '2030-02-04',
    });

    expect(survey.requiresIdentityConfirmation()).toBe(true);
    expect(survey.identityForm.controls.identidadCorrecta.hasError('required')).toBe(true);
  });
  it('does not confirm when a previous visible section is invalid', () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesEducacionSuperior: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    survey.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    survey.regulationForm.controls.aceptaReglamento.setValue(true);
    survey.activeSection.set('reglamento');

    survey.continue();

    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(survey.activeSection()).toBe('identidad');
    expect(survey.preEnrollmentError()).toBe(
      'Completá la información pendiente antes de confirmar la preinscripción.'
    );
  });

  it('uploads touched identity files before confirming pre-enrollment', () => {
    const { survey, forms } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesEducacionSuperior: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    const frente = new File(['front'], 'frente.png', { type: 'image/png' });
    const dorso = new File(['back'], 'dorso.png', { type: 'image/png' });
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    survey.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    survey.identityForm.controls.vencimientoDocumento.markAsDirty();
    survey.updateIdentityFile('frente', fileEvent(frente));
    survey.updateIdentityFile('dorso', fileEvent(dorso));
    survey.updateIdentityFile('selfie', fileEvent(selfie));
    survey.regulationForm.controls.aceptaReglamento.setValue(true);
    forms.academicForm.controls.turno.setValue('300');

    survey.continue();

    expect(uploadIdentityDocument).toHaveBeenCalledWith({
      fecha: '2030-02-04',
      frente,
      dorso,
    });
    expect(uploadIdentityPhoto).toHaveBeenCalledWith(selfie);
    expect(confirmPreEnrollment).toHaveBeenCalledWith({
      aceptoReglamento: true,
      idOfertaSeleccionada: 300,
    });
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
    forms: InscripcionFormsStore;
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
            getStudentRegulationAcceptance,
            getIdentityPreload,
            getInitialSurvey: () => of(initialSurvey),
            saveInitialSurvey,
            uploadIdentityDocument,
            uploadIdentityPhoto,
            confirmPreEnrollment,
            registerProductInterest: vi.fn(),
          },
        },
      ],
    });
    return {
      survey: TestBed.inject(InscripcionSurveyFacade),
      process: TestBed.inject(InscripcionProcessStore),
      forms: TestBed.inject(InscripcionFormsStore),
    };
  }

  function preloadFile(name: string): File {
    const bytes = new Uint8Array([1, 2, 3]).buffer;
    return {
      name,
      size: 3,
      type: 'image/png',
      arrayBuffer: () => Promise.resolve(bytes),
    } as File;
  }
  function applyIdentityPreload(
    survey: InscripcionSurveyFacade,
    preload: InscripcionIdentityPreload
  ): void {
    (
      survey as unknown as {
        applyIdentityPreload(preload: InscripcionIdentityPreload): void;
      }
    ).applyIdentityPreload(preload);
  }
  function fileEvent(file: File): Parameters<InscripcionSurveyFacade['updateIdentityFile']>[1] {
    return { value: [{ isValid: true, file }] } as Parameters<
      InscripcionSurveyFacade['updateIdentityFile']
    >[1];
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
      estadoEducacionSuperiorPreviaId: null,
      tieneEducacionSuperior: null,
      nivelFormacionMadreId: null,
      nivelFormacionPadreId: null,
      madreEgresadaOrt: null,
      padreEgresadoOrt: null,
      anioDecisionCarreraId: null,
      anioDecisionOrtId: null,
      seInformoEnOtrasUniversidades: null,
      apoyoDecisionId: null,
      apoyoPadres: null,
      apoyoOtros: null,
      apoyoAmigosFamiliares: null,
      apoyoNadie: null,
      apoyoAmigoPropuesta: null,
      nivelDecisionId: null,
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
