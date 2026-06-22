import '@angular/compiler';

import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import type { OrtFileUploaderChange } from '@desarrolloort/components';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../catalogs/services/catalogs';
import type { InscripcionInitialSurveyResolved } from '../resolvers/inscripcion-initial-survey.resolver';
import { Inscripciones } from '../services/inscripciones';
import { InscripcionFlowFacade } from './inscripcion-flow.facade';

describe('InscripcionFlowFacade', () => {
  let facade: InscripcionFlowFacade;
  let catalogsMock: ReturnType<typeof createCatalogsMock>;
  let inscripcionesMock: ReturnType<typeof createInscripcionesMock>;

  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    catalogsMock = createCatalogsMock();
    inscripcionesMock = createInscripcionesMock();
    facade = createFacade();
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('starts in proposal with the first-time scenario by default', () => {
    expect(facade.surveyState()).toBe('no-iniciada');
    expect(facade.screen()).toBe('propuesta');
    expect(facade.stepNumber()).toBe(1);
    expect(facade.visibleSections()).toEqual([
      'educacion',
      'decision-academica',
      'experiencia-ort',
      'situacion-laboral',
      'identidad',
      'reglamento',
    ]);
  });

  it('loads catalog-backed proposal and survey options', () => {
    expect(facade.proposalOptions()).toEqual([
      {
        value: '1',
        label: 'Carrera universitaria',
        icon: 'school',
        hint: 'Formación de grado con enfoque práctico y salida laboral',
      },
      {
        value: '2',
        label: 'Tecnicatura',
        icon: 'list_alt',
        hint: 'Carreras cortas, prácticas y orientadas al mercado',
      },
      {
        value: '3',
        label: 'Actualización profesional',
        icon: 'how_to_reg',
        hint: 'Cursos cortos para actualizar habilidades',
      },
    ]);
    expect(facade.previousCareerOptions()).toEqual([
      { value: '3', label: 'No cursé estudios superiores' },
    ]);
    expect(facade.educationLevelOptions()).toEqual([
      { value: '4', label: 'Universitaria completa' },
    ]);
    expect(facade.careerDecisionOptions()).toEqual([{ value: '1', label: 'Durante secundaria' }]);
    expect(facade.supportOptions()).toEqual([{ value: '5', label: 'Familia' }]);
    expect(facade.motivesOptions()).toEqual([{ value: '2', label: 'Prestigio académico' }]);
  });

  it('filters careers by the selected proposal level group', () => {
    facade.academicForm.controls.tipoPropuesta.setValue('1');
    expect(facade.careerOptions()).toEqual([{ value: '20', label: 'Ingeniería en Sistemas' }]);

    facade.academicForm.controls.tipoPropuesta.setValue('2');
    expect(facade.careerOptions()).toEqual([{ value: '30', label: 'Tecnicatura en Diseño' }]);

    facade.academicForm.controls.tipoPropuesta.setValue('3');
    expect(facade.careerOptions()).toEqual([
      { value: '40', label: 'Programa ejecutivo en Data Analytics' },
      { value: '50', label: 'Curso de actualización profesional' },
    ]);
  });

  it('loads starts and shifts when the academic selection changes', () => {
    facade.academicForm.controls.carrera.setValue('20');
    expect(catalogsMock.getComienzos).toHaveBeenCalledWith(20);
    expect(facade.startOptions()).toEqual([{ value: '200', label: 'Agosto 2026' }]);

    facade.academicForm.controls.comienzo.setValue('200');
    expect(catalogsMock.getTurnos).toHaveBeenCalledWith(20, 200);
    expect(facade.turnoOptions()).toEqual([{ value: '300', label: 'Nocturno (19:00 a 23:00)' }]);
  });

  it('reports dependent catalog loading and keeps unavailable selects disabled', () => {
    const careers$ = new Subject<
      Array<{
        idProducto: number;
        idNivelProducto: number;
        nombreProducto: string;
        nombreNivelProducto: string;
      }>
    >();
    const starts$ = new Subject<Array<{ idProceso: number; nombreProceso: string }>>();
    const turnos$ = new Subject<
      Array<{
        idOferta: number;
        idTurno: number;
        nombreTurno: string;
        horarioReferencia: string;
      }>
    >();
    catalogsMock.getCareers.mockReturnValueOnce(careers$);
    catalogsMock.getComienzos.mockReturnValueOnce(starts$);
    catalogsMock.getTurnos.mockReturnValueOnce(turnos$);
    facade = createFacade();

    expect(facade.loadingCareers()).toBe(true);
    expect(facade.careersLoadingMessage()).toBe('Estamos cargando las carreras.');

    careers$.next([
      {
        idProducto: 20,
        idNivelProducto: 1,
        nombreProducto: 'Ingeniería en Sistemas',
        nombreNivelProducto: 'Carrera universitaria',
      },
    ]);
    careers$.complete();

    expect(facade.loadingCareers()).toBe(false);

    facade.academicForm.controls.tipoPropuesta.setValue('1');
    facade.academicForm.controls.carrera.setValue('20');

    expect(facade.loadingStarts()).toBe(true);
    expect(facade.canSelectStart()).toBe(false);
    expect(facade.startsLoadingMessage()).toBe(
      'Estamos cargando los comienzos para "Ingeniería en Sistemas".'
    );

    starts$.next([{ idProceso: 200, nombreProceso: 'Agosto 2026' }]);
    starts$.complete();

    expect(facade.loadingStarts()).toBe(false);
    expect(facade.canSelectStart()).toBe(true);

    facade.academicForm.controls.comienzo.setValue('200');

    expect(facade.loadingTurnos()).toBe(true);
    expect(facade.canSelectTurno()).toBe(false);
    expect(facade.turnosLoadingMessage()).toBe('Estamos cargando los turnos para "Agosto 2026".');

    turnos$.next([
      {
        idOferta: 300,
        idTurno: 10,
        nombreTurno: 'Nocturno',
        horarioReferencia: '19:00 a 23:00',
      },
    ]);
    turnos$.complete();

    expect(facade.loadingTurnos()).toBe(false);
    expect(facade.canSelectTurno()).toBe(true);
  });

  it('advances to the first survey section after a valid proposal', () => {
    fillAcademicForm();
    facade.continue();

    expect(inscripcionesMock.registerProductInterest).toHaveBeenCalledWith({
      idOferta: 300,
      idProcesoSeleccionado: 200,
      idProducto: 20,
    });
    expect(facade.screen()).toBe('encuesta');
    expect(facade.activeSection()).toBe('educacion');
    expect(facade.stepNumber()).toBe(2);
  });

  it('does not advance when product interest registration fails', () => {
    inscripcionesMock.registerProductInterest.mockReturnValueOnce(
      throwError(() => new Error('network'))
    );
    fillAcademicForm();

    facade.continue();

    expect(facade.screen()).toBe('propuesta');
    expect(facade.academicErrors()).toContainEqual({
      message: 'No se pudo registrar el interés por la propuesta seleccionada.',
    });
  });

  it('resumes the partial scenario at the first incomplete section', () => {
    inscripcionesMock.getInitialSurvey.mockReturnValueOnce(
      of(createInitialSurvey('decision-academica'))
    );
    facade = createFacade('parcial');

    expect(facade.screen()).toBe('encuesta');
    expect(facade.activeSection()).toBe('decision-academica');
    expect(facade.getSectionState('educacion')).toBe('completa');
    expect(facade.educationForm.getRawValue()).toEqual({
      cursaSecundaria: 'cursando',
      lugarSecundaria: '',
      estadoEducacionSuperior: '3',
      formacionMadre: '4',
      formacionPadre: '4',
    });
  });

  it('uses the route resolver state without reloading the initial survey', () => {
    inscripcionesMock.getInitialSurvey.mockClear();

    facade = createFacade(undefined, undefined, {
      initialSurvey: createInitialSurvey('decision-academica'),
      loadFailed: false,
    });

    expect(inscripcionesMock.getInitialSurvey).not.toHaveBeenCalled();
    expect(facade.screen()).toBe('encuesta');
    expect(facade.activeSection()).toBe('decision-academica');
  });

  it('skips survey sections and survey saving when the person has no survey right', () => {
    inscripcionesMock.getInitialSurvey.mockReturnValueOnce(of({ tieneDerechoEncuesta: false }));
    facade = createFacade();

    expect(facade.hasInitialSurveyRight()).toBe(false);
    expect(facade.visibleSections()).toEqual(['identidad', 'reglamento']);
    expect(facade.screen()).toBe('propuesta');

    fillAcademicForm();
    facade.continue();

    expect(facade.screen()).toBe('encuesta');
    expect(facade.activeSection()).toBe('identidad');

    fillIdentityForm();
    facade.continue();
    facade.regulationForm.controls.aceptaReglamento.setValue(true);
    facade.continue();

    expect(inscripcionesMock.saveInitialSurvey).not.toHaveBeenCalled();
    expect(inscripcionesMock.confirmPreEnrollment).toHaveBeenCalledWith({
      aceptoReglamento: true,
      idOfertaSeleccionada: 300,
    });
    expect(facade.screen()).toBe('pago');
  });

  it('marks regulation as accepted when the backend says it was already signed', () => {
    inscripcionesMock.getStudentRegulationAcceptance.mockReturnValue(
      of({ aceptoReglamentoEstudiantil: true, fechaAceptacion: '2026-06-01' })
    );

    facade = createFacade();

    expect(facade.hasAcceptedStudentRegulation()).toBe(true);
    expect(facade.regulationForm.controls.aceptaReglamento.value).toBe(true);
    expect(facade.getSectionState('reglamento')).toBe('completa');
  });

  it('hides historical survey sections when the survey is already complete', () => {
    inscripcionesMock.getInitialSurvey.mockReturnValueOnce(of(createInitialSurvey('completa')));
    facade = createFacade('encuesta-completa');

    expect(facade.visibleSections()).toEqual(['identidad', 'reglamento']);
    expect(facade.activeSection()).toBe('identidad');
    expect(facade.surveyState()).toBe('completa');
  });

  it('preloads identity files and expiration when identity verification is reached', async () => {
    inscripcionesMock.getIdentityPreload.mockReturnValueOnce(
      of({
        frente: new File(['front'], 'frente-backend.png', { type: 'image/png' }),
        dorso: new File(['back'], 'dorso-backend.png', { type: 'image/png' }),
        selfie: new File(['photo'], 'foto-persona.jpg', { type: 'image/jpeg' }),
        fechaVencimiento: '2030-02-04',
      })
    );

    facade = createFacade(undefined, undefined, {
      initialSurvey: createInitialSurvey('completa'),
      loadFailed: false,
    });
    TestBed.flushEffects();

    await flushPromises();

    expect(inscripcionesMock.getIdentityPreload).toHaveBeenCalledOnce();
    expect(facade.identityFiles().frente?.name).toBe('frente-backend.png');
    expect(facade.identityFiles().dorso?.name).toBe('dorso-backend.png');
    expect(facade.identityFiles().selfie?.name).toBe('foto-persona.jpg');
    expect(facade.initialIdentityFiles().frente[0]?.name).toBe('frente-backend.png');
    expect(facade.initialIdentityFiles().frente[0]?.src).toBeInstanceOf(ArrayBuffer);
    expect(facade.identityForm.controls.vencimientoDocumento.value).toEqual(new Date(2030, 1, 4));
  });

  it('requests identity preload even when the initial survey is null', async () => {
    facade = createFacade(undefined, undefined, {
      initialSurvey: { tieneDerechoEncuesta: true },
      loadFailed: false,
    });

    fillAcademicForm();
    facade.continue();
    fillEducationForm();
    facade.continue();
    fillAcademicDecisionForm();
    facade.continue();
    fillOrtExperienceForm();
    facade.continue();
    fillWorkForm();
    facade.continue();
    TestBed.flushEffects();

    expect(facade.activeSection()).toBe('identidad');
    await flushPromises();

    expect(inscripcionesMock.getIdentityPreload).toHaveBeenCalledOnce();
  });

  it('does not overwrite identity values selected while preload is pending', () => {
    const preload$ = new Subject<{
      frente: File | null;
      dorso: File | null;
      selfie: File | null;
      fechaVencimiento: string | null;
    }>();
    inscripcionesMock.getIdentityPreload.mockReturnValueOnce(preload$);
    facade = createFacade(undefined, undefined, {
      initialSurvey: createInitialSurvey('completa'),
      loadFailed: false,
    });
    TestBed.flushEffects();

    const selectedExpiration = new Date(2031, 5, 10);
    facade.updateIdentityFile('frente', fileChange('frente-usuario.png'));
    facade.identityForm.controls.vencimientoDocumento.setValue(selectedExpiration);
    facade.identityForm.controls.vencimientoDocumento.markAsDirty();

    preload$.next({
      frente: new File(['front'], 'frente-backend.png', { type: 'image/png' }),
      dorso: new File(['back'], 'dorso-backend.png', { type: 'image/png' }),
      selfie: null,
      fechaVencimiento: '2030-02-04',
    });

    expect(facade.identityFiles().frente?.name).toBe('frente-usuario.png');
    expect(facade.identityFiles().dorso?.name).toBe('dorso-backend.png');
    expect(facade.identityForm.controls.vencimientoDocumento.value).toBe(selectedExpiration);
  });
  it('validates and completes the active survey section', () => {
    fillAcademicForm();
    facade.continue();
    fillEducationForm();

    expect(facade.getSectionState('educacion')).toBe('completa');
    expect(facade.activeSection()).toBe('educacion');

    facade.continue();

    expect(facade.getSectionState('educacion')).toBe('completa');
    expect(facade.activeSection()).toBe('decision-academica');
  });

  it('saves the survey in the backend without including identity files', () => {
    fillAcademicForm();
    facade.continue();
    facade.openSection('identidad');
    facade.workForm.controls.situacionLaboral.setValue('trabaja');
    facade.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    facade.updateIdentityFile('frente', fileChange('frente.png'));
    facade.requestExit();
    facade.confirmExit();

    const payload = inscripcionesMock.saveInitialSurvey.mock.calls[0]?.[0];
    const serializedPayload = JSON.stringify(payload);

    expect(payload).toMatchObject({
      idProducto: 20,
      idProceso: 200,
      ultimoAnioSecundaria: 1,
      trabajaActualmente: 'trabaja',
    });
    expect(serializedPayload).not.toContain('frente.png');
    expect(serializedPayload).not.toContain('binary');
  });

  it('saves the final survey and confirms pre-enrollment before payment', () => {
    fillAcademicForm();
    facade.continue();
    fillEducationForm();
    facade.continue();
    fillAcademicDecisionForm();
    facade.continue();
    fillOrtExperienceForm();
    facade.continue();
    fillWorkForm();
    facade.continue();
    fillIdentityForm();
    facade.continue();
    facade.regulationForm.controls.aceptaReglamento.setValue(true);

    facade.continue();

    expect(inscripcionesMock.saveInitialSurvey).toHaveBeenCalledWith(
      expect.objectContaining({
        idProducto: 20,
        idProceso: 200,
        ultimoAnioSecundaria: 1,
        trabajaActualmente: 'trabaja',
      })
    );
    expect(inscripcionesMock.confirmPreEnrollment).toHaveBeenCalledWith({
      aceptoReglamento: true,
      idOfertaSeleccionada: 300,
    });
    expect(inscripcionesMock.saveInitialSurvey.mock.invocationCallOrder[0]).toBeLessThan(
      inscripcionesMock.confirmPreEnrollment.mock.invocationCallOrder[0]
    );
    expect(facade.screen()).toBe('pago');
    expect(facade.surveyState()).toBe('completa');
    expect(facade.inscriptionAmount()).toBe('$ 21.000');
    expect(facade.paymentDeadline()).toBe('15/04/2027');
    expect(facade.summaryItems()[0].value).toBe('Licenciatura en Diseño Gráfico');
  });

  it('routes offline payments to their reservation result', () => {
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    facade.screen.set('pago');
    facade.paymentForm.controls.metodoPago.setValue('paganza');
    facade.requestPaymentConfirmation();
    facade.confirmPayment();

    expect(facade.screen()).toBe('reserva');
    expect(facade.reservationInstructions().items).toContain(
      'Ingresá tu número de estudiante: 397654'
    );
    expect(consoleSpy).toHaveBeenCalledOnce();
  });

  it('processes immediate payments before showing confirmation', () => {
    vi.useFakeTimers();
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
    facade.screen.set('pago');
    facade.paymentForm.controls.metodoPago.setValue('tarjeta-credito');
    facade.requestPaymentConfirmation();
    facade.confirmPayment();

    expect(facade.screen()).toBe('procesando');
    vi.advanceTimersByTime(1000);
    expect(facade.screen()).toBe('inscripcion-confirmada');
  });

  it('forces the in-process result through the query parameter', () => {
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
    facade = createFacade('primera-vez', 'en-proceso');
    facade.screen.set('pago');
    facade.paymentForm.controls.metodoPago.setValue('cuenta-bancaria');
    facade.requestPaymentConfirmation();
    facade.confirmPayment();

    expect(facade.screen()).toBe('inscripcion-en-proceso');
  });

  it('does not return to payment when the dialog closes after a terminal result', () => {
    facade.screen.set('inscripcion-en-proceso');

    facade.cancelPaymentConfirmation();

    expect(facade.screen()).toBe('inscripcion-en-proceso');
  });

  it('returns completed surveys to identity verification', () => {
    inscripcionesMock.getInitialSurvey.mockReturnValueOnce(of(createInitialSurvey('completa')));
    facade = createFacade('encuesta-completa');

    expect(facade.screen()).toBe('encuesta');
    expect(facade.activeSection()).toBe('identidad');
    expect(facade.getSectionState('identidad')).toBe('activa');
  });

  function createFacade(
    escenario?: string,
    resultado?: string,
    initialSurvey?: InscripcionInitialSurveyResolved
  ): InscripcionFlowFacade {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        InscripcionFlowFacade,
        { provide: Catalogs, useValue: catalogsMock },
        { provide: Inscripciones, useValue: inscripcionesMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: initialSurvey ? { initialSurvey } : {},
              queryParamMap: convertToParamMap({ escenario, resultado }),
            },
          },
        },
      ],
    });

    return TestBed.inject(InscripcionFlowFacade);
  }

  function fillAcademicForm(): void {
    facade.academicForm.setValue({
      tipoPropuesta: '1',
      carrera: '20',
      comienzo: '200',
      turno: '300',
    });
  }

  function fillEducationForm(): void {
    facade.educationForm.setValue({
      cursaSecundaria: 'cursando',
      lugarSecundaria: 'uruguay',
      estadoEducacionSuperior: '3',
      formacionMadre: '4',
      formacionPadre: '4',
    });
  }

  function fillAcademicDecisionForm(): void {
    facade.academicDecisionForm.setValue({
      anioDecisionCarrera: '1',
      apoyoDecision: '5',
      anioDecisionOrt: '2-ems',
      otrasUniversidades: 'si',
      certezaDecision: 'decidido',
      motivosOrt: '2',
    });
  }

  function fillOrtExperienceForm(): void {
    facade.ortExperienceForm.setValue({
      reunionAsesoramiento: 'si',
      visitoWeb: 'si',
      visitoSede: 'si',
      recuerdaPublicidad: 'si',
    });
  }

  function fillWorkForm(): void {
    facade.workForm.controls.situacionLaboral.setValue('trabaja');
  }

  function fillIdentityForm(): void {
    facade.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    facade.updateIdentityFile('frente', fileChange('frente.png'));
    facade.updateIdentityFile('dorso', fileChange('dorso.png'));
    facade.updateIdentityFile('selfie', fileChange('rostro.png'));
  }
});

function flushPromises(): Promise<void> {
  return new Promise(resolve => setTimeout(resolve));
}

function createInitialSurvey(state: string) {
  return {
    tieneDerechoEncuesta: true,
    encuesta: {
      idEncuestaIni: 1,
      idProducto: 20,
      idProceso: 200,
      idTurno: 10,
      estadoEncuestaIniAdmision: state,
      ultimoanioSecundariaEncuestaIni: true,
      tieneEducacionSuperiorEncuestaIni: 'N',
      instruccionMadreEncuestaIni: '4',
      instruccionPadreEncuestaIni: '4',
    },
  };
}

function createCatalogsMock() {
  return {
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
        {
          idProducto: 40,
          idNivelProducto: 3,
          nombreProducto: 'Programa ejecutivo en Data Analytics',
          nombreNivelProducto: 'Actualización profesional',
        },
        {
          idProducto: 50,
          idNivelProducto: 4,
          nombreProducto: 'Curso de actualización profesional',
          nombreNivelProducto: 'Cursos',
        },
      ])
    ),
    getComienzos: vi.fn().mockReturnValue(of([{ idProceso: 200, nombreProceso: 'Agosto 2026' }])),
    getInitialSurveyCatalogs: vi.fn().mockReturnValue(
      of({
        aniosAprobadosEducacionSuperior: [],
        compartidoCon: [{ id: 5, label: 'Familia' }],
        decisionCarrera: [{ id: 1, label: 'Durante secundaria' }],
        decisionUniversidad: [{ id: 2, label: 'Prestigio académico' }],
        estadoEducacionSuperior: [{ id: 3, label: 'No cursé estudios superiores' }],
        formacionTutores: [{ id: 4, label: 'Universitaria completa' }],
        nivelConocimiento: [{ id: 6, label: 'Conocía bien la propuesta' }],
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
}

function createInscripcionesMock() {
  return {
    confirmPreEnrollment: vi.fn().mockReturnValue(
      of({
        confirmada: true,
        fechaVencimientoPago: '2027-04-15',
        seniaInscripcion: 21000,
        resumen: {
          carrera: 'Licenciatura en Diseño Gráfico',
          comienzo: 'Marzo 2027',
          turno: 'Matutino',
        },
      })
    ),
    getIdentityPreload: vi
      .fn()
      .mockReturnValue(of({ frente: null, dorso: null, selfie: null, fechaVencimiento: null })),
    getInitialSurvey: vi.fn().mockReturnValue(of({})),
    getStudentRegulationAcceptance: vi
      .fn()
      .mockReturnValue(of({ aceptoReglamentoEstudiantil: false })),
    saveInitialSurvey: vi.fn().mockReturnValue(of(true)),
    registerProductInterest: vi.fn().mockReturnValue(of(true)),
  };
}

function fileChange(name: string): OrtFileUploaderChange {
  const file = new File(['binary'], name, { type: 'image/png' });
  const info = {
    file,
    id: name,
    name,
    size: file.size,
    type: file.type,
    extension: 'png',
    displaySize: '6 B',
    isValid: true,
    errors: [],
    uploadStatus: 'idle' as const,
  };

  return {
    value: [info],
    addedFiles: [info],
    removedFiles: [],
  } as unknown as OrtFileUploaderChange;
}
