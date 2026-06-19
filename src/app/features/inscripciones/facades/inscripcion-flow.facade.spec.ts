import '@angular/compiler';

import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import type { OrtFileUploaderChange } from '@desarrolloort/components';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../catalogs/services/catalogs';
import type { BorradorInscripcion } from '../models/inscripcion-flow';
import { InscripcionDraft } from '../services/inscripcion-draft';
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
    expect(facade.scenario).toBe('primera-vez');
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
    expect(facade.knowledgeOptions()).toEqual([{ value: '6', label: 'Conocía bien la propuesta' }]);
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
    facade = createFacade('parcial');

    expect(facade.screen()).toBe('encuesta');
    expect(facade.activeSection()).toBe('decision-academica');
    expect(facade.getSectionState('educacion')).toBe('completa');
  });

  it('hides historical survey sections when the survey is already complete', () => {
    facade = createFacade('encuesta-completa');
    fillAcademicForm();
    facade.continue();

    expect(facade.visibleSections()).toEqual(['identidad', 'reglamento']);
    expect(facade.activeSection()).toBe('identidad');
    expect(facade.surveyState()).toBe('completa');
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

  it('does not persist identity files in the draft', () => {
    fillAcademicForm();
    facade.continue();
    facade.openSection('identidad');
    facade.identityForm.controls.vencimientoDocumento.setValue(new Date(2030, 1, 4));
    facade.updateIdentityFile('frente', fileChange('frente.png'));

    const storedDraft = sessionStorage.getItem('inscripcion-borrador:v1:anonimo:primera-vez') ?? '';

    expect(storedDraft).toContain('"vencimientoDocumento":"2030-02-04"');
    expect(storedDraft).not.toContain('frente.png');
    expect(storedDraft).not.toContain('binary');
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

  it('returns to identity when a restored draft no longer has its files', () => {
    sessionStorage.setItem(
      'inscripcion-borrador:v1:anonimo:encuesta-completa',
      JSON.stringify(createPaymentDraft())
    );

    facade = createFacade('encuesta-completa');

    expect(facade.screen()).toBe('encuesta');
    expect(facade.activeSection()).toBe('identidad');
    expect(facade.getSectionState('identidad')).toBe('activa');
  });

  function createFacade(escenario?: string, resultado?: string): InscripcionFlowFacade {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        InscripcionDraft,
        InscripcionFlowFacade,
        { provide: Catalogs, useValue: catalogsMock },
        { provide: Inscripciones, useValue: inscripcionesMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
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
});

function createPaymentDraft(): BorradorInscripcion {
  return {
    version: 1,
    escenario: 'encuesta-completa',
    pantalla: 'pago',
    seccionActiva: 'reglamento',
    seccionesCompletas: ['identidad', 'reglamento'],
    propuesta: {
      tipoPropuesta: '1',
      carrera: '20',
      comienzo: '200',
      turno: '300',
    },
    encuesta: {
      educacion: {
        cursaSecundaria: '',
        lugarSecundaria: '',
        estadoEducacionSuperior: '',
        formacionMadre: '',
        formacionPadre: '',
      },
      decisionAcademica: {
        anioDecisionCarrera: '',
        apoyoDecision: '',
        anioDecisionOrt: '',
        otrasUniversidades: '',
        certezaDecision: '',
        motivosOrt: '',
      },
      experienciaOrt: {
        reunionAsesoramiento: '',
        visitoWeb: '',
        visitoSede: '',
        recuerdaPublicidad: '',
      },
      situacionLaboral: { situacionLaboral: '' },
    },
    identidad: { vencimientoDocumento: '2030-02-04' },
    reglamento: { aceptaReglamento: true },
    pago: { metodoPago: '' },
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
