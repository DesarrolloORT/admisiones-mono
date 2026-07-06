import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
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
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
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
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });

    expect(survey.visibleSections()).toEqual(['identidad', 'reglamento']);
    await expect(firstValueFrom(survey.savePartial())).resolves.toBe(true);
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });

  it('completes identity and advances when confirming a complete backend preload', () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
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

    survey.identityForm.controls.identidadCorrecta.setValue(true);

    expect(survey.getSectionState('identidad')).toBe('completa');
    expect(survey.activeSection()).toBe('reglamento');
  });

  it('does not save the survey when work becomes complete', () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: true,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    survey.activeSection.set('situacion-laboral');
    survey.workForm.controls.situacionLaboral.setValue('no-trabaja');

    expect(survey.getSectionState('situacion-laboral')).toBe('completa');
    expect(saveInitialSurvey).not.toHaveBeenCalled();
  });
  it('does not confirm when a previous visible section is invalid', () => {
    const { survey } = createFacade({
      tieneDerechoEncuesta: false,
      encuesta: null,
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
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
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
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

  it('waits for document and photo before saving the survey and confirming', () => {
    const documentResult = new Subject<boolean>();
    const photoResult = new Subject<boolean>();
    uploadIdentityDocument.mockReturnValue(documentResult);
    uploadIdentityPhoto.mockReturnValue(photoResult);
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(uploadIdentityDocument).toHaveBeenCalledOnce();
    expect(uploadIdentityPhoto).toHaveBeenCalledOnce();
    expect(saveInitialSurvey).not.toHaveBeenCalled();

    documentResult.next(true);
    documentResult.complete();
    expect(saveInitialSurvey).not.toHaveBeenCalled();

    photoResult.next(true);
    photoResult.complete();

    expect(saveInitialSurvey).toHaveBeenCalledOnce();
    expect(confirmPreEnrollment).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('pago');
  });

  it.each([
    { failure: 'data false', result: of(false) },
    { failure: 'HTTP 400', result: throwError(() => ({ status: 400 })) },
  ])('reopens identity and stops the chain when photo returns $failure', ({ result }) => {
    uploadIdentityPhoto.mockReturnValue(result);
    const { survey, frente, dorso, selfie } = prepareFinalizableSurvey();

    survey.continue();

    expect(saveInitialSurvey).not.toHaveBeenCalled();
    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(survey.activeSection()).toBe('identidad');
    expect(survey.getSectionState('identidad')).toBe('activa');
    expect(survey.identityFiles()).toEqual({ frente, dorso, selfie });
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo guardar la verificación de identidad. Intentá nuevamente.'
    );
  });

  it.each([
    { failure: 'data false', result: of(false) },
    { failure: 'HTTP 400', result: throwError(() => ({ status: 400 })) },
  ])('does not confirm when the final survey save returns $failure', ({ result }) => {
    saveInitialSurvey.mockReturnValue(result);
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(confirmPreEnrollment).not.toHaveBeenCalled();
    expect(process.flow.currentStep()).toBe('encuesta');
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo guardar y confirmar la preinscripción. Intentá nuevamente.'
    );
  });

  it('does not navigate when pre-enrollment is not confirmed', () => {
    confirmPreEnrollment.mockReturnValue(
      of({
        confirmada: false,
        fechaVencimientoPago: null,
        seniaInscripcion: null,
        saldoCuenta: null,
        resumen: null,
      })
    );
    const { survey, process } = prepareFinalizableSurvey();

    survey.continue();

    expect(confirmPreEnrollment).toHaveBeenCalledOnce();
    expect(process.flow.currentStep()).toBe('encuesta');
    expect(survey.preEnrollmentError()).toBe(
      'No se pudo confirmar la preinscripción. Intentá nuevamente.'
    );
  });

  it('uses school year orientations directly from the selected year catalog', () => {
    const { survey } = createFacade(
      {
        tieneDerechoEncuesta: true,
        encuesta: null,
        universidadesConsideradas: [],
        universidadesConsideradasOtros: [],
        universidadesEducacionSuperior: [],
        universidadesEducacionSuperiorOtros: [],
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
              baccalaureates: [{ id: 20, label: 'Bachillerato A', orientation: 'Cientifico' }],
            },
            {
              id: 12,
              label: '3 EMS',
              baccalaureates: [{ id: 30, label: 'Bachillerato B', orientation: 'Economia' }],
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

    expect(survey.shouldAskBaccalaureateOrientation()).toBe(false);
    expect(survey.orientationOptions()).toEqual([]);

    survey.educationForm.controls.anioSecundaria.setValue('11');

    expect(survey.shouldAskBaccalaureateOrientation()).toBe(true);
    expect(survey.orientationOptions()).toEqual([{ value: '20', label: 'Cientifico' }]);

    survey.educationForm.controls.orientacion.setValue('20');
    survey.educationForm.controls.anioSecundaria.setValue('12');

    expect(survey.orientationOptions()).toEqual([{ value: '30', label: 'Economia' }]);
    expect(survey.educationForm.controls.orientacion.value).toBe('');
  });

  it('requires free-text details for recursado and Otro university options', () => {
    const { survey } = createFacade(
      {
        tieneDerechoEncuesta: true,
        encuesta: null,
        universidadesConsideradas: [],
        universidadesConsideradasOtros: [],
        universidadesEducacionSuperior: [],
        universidadesEducacionSuperiorOtros: [],
        opcionesMotivosSeleccionados: [],
        opcionesPublicidadSeleccionadas: [],
      },
      {
        educacion: {
          ubicacionesUltimoAnioSecundaria: [],
          aniosBachillerato: [],
          estadosEducacionSuperiorPrevia: [],
          universidades: [
            { id: 0, label: 'Otra' },
            { id: 10, label: 'Udelar' },
          ],
          nivelesFormacionTutores: [],
        },
        decisionAcademica: {
          aniosEducacionMediaSuperior: [],
          apoyosDecision: [],
          nivelesDecision: [],
          universidades: [
            { id: 0, label: 'Otra' },
            { id: 11, label: 'UCU' },
          ],
          motivosEleccionOrt: [],
        },
      }
    );

    survey.educationForm.controls.recursaAnioBachillerato.setValue('si');
    expect(survey.shouldAskRecursaCount()).toBe(true);
    expect(survey.educationForm.controls.vecesRecursaAnioBachillerato.hasError('required')).toBe(
      true
    );
    survey.educationForm.controls.vecesRecursaAnioBachillerato.setValue(0);
    expect(survey.educationForm.controls.vecesRecursaAnioBachillerato.hasError('min')).toBe(true);
    survey.educationForm.controls.vecesRecursaAnioBachillerato.setValue(2);
    expect(survey.educationForm.controls.vecesRecursaAnioBachillerato.valid).toBe(true);

    survey.educationForm.patchValue({
      estadoEducacionSuperior: '1',
      universidadesEducacionSuperior: ['0'],
    });
    expect(survey.shouldAskHigherEducationOtherUniversity()).toBe(true);
    expect(
      survey.educationForm.controls.universidadEducacionSuperiorOtro.hasError('required')
    ).toBe(true);
    survey.educationForm.controls.universidadEducacionSuperiorOtro.setValue('Otra superior');
    expect(survey.educationForm.controls.universidadEducacionSuperiorOtro.valid).toBe(true);

    survey.academicDecisionForm.patchValue({
      otrasUniversidades: 'si',
      universidadesInformadas: ['0'],
    });
    expect(survey.shouldAskInformedOtherUniversity()).toBe(true);
    expect(survey.academicDecisionForm.controls.universidadInformadaOtro.hasError('required')).toBe(
      true
    );
    survey.academicDecisionForm.controls.universidadInformadaOtro.setValue('Otra consultada');
    expect(survey.academicDecisionForm.controls.universidadInformadaOtro.valid).toBe(true);
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

  function prepareFinalizableSurvey() {
    const result = createFacade({
      tieneDerechoEncuesta: true,
      encuesta: createInitialSurvey({ completa: true }),
      universidadesConsideradas: [],
      universidadesConsideradasOtros: [],
      universidadesEducacionSuperior: [],
      universidadesEducacionSuperiorOtros: [],
      opcionesMotivosSeleccionados: [],
      opcionesPublicidadSeleccionadas: [],
    });
    const frente = new File(['front'], 'frente.png', { type: 'image/png' });
    const dorso = new File(['back'], 'dorso.png', { type: 'image/png' });
    const selfie = new File(['photo'], 'selfie.png', { type: 'image/png' });

    result.survey.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    result.survey.identityForm.controls.vencimientoDocumento.markAsDirty();
    result.survey.updateIdentityFile('frente', fileEvent(frente));
    result.survey.updateIdentityFile('dorso', fileEvent(dorso));
    result.survey.updateIdentityFile('selfie', fileEvent(selfie));
    result.survey.regulationForm.controls.aceptaReglamento.setValue(true);
    result.forms.academicForm.controls.turno.setValue('300');

    return { ...result, frente, dorso, selfie };
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
      recursaAnioBachillerato: null,
      vecesRecursaAnioBachillerato: null,
      institucionSecundariaId: null,
      ubicacionSecundariaId: null,
      nombreInstitucionSecundaria: null,
      estadoEducacionSuperiorPreviaId: null,
      nivelFormacionMadreId: null,
      nivelFormacionPadreId: null,
      madreEgresadaOrt: null,
      padreEgresadoOrt: null,
      anioDecisionCarreraId: null,
      anioDecisionOrtId: null,
      seInformoEnOtrasUniversidades: null,
      apoyoDecisionId: null,
      nivelDecisionId: null,
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
