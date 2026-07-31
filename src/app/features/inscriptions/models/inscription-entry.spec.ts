import type { InscripcionDetail, InscripcionSummary } from './inscription-detail';
import {
  deriveInitialInscripcionState,
  EMPTY_INITIAL_SURVEY_RESPONSE,
  type InscripcionEntryResolved,
  type InscripcionInitialState,
  type InscripcionInitialSurveyResolved,
} from './inscription-entry';
import type {
  InscripcionInitialSurvey,
  InscripcionInitialSurveyResponse,
  InscripcionOfertaResumen,
} from './inscription-flow';

// --- Fixtures ---------------------------------------------------------------

function survey(values: Partial<InscripcionInitialSurvey> = {}): InscripcionInitialSurvey {
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

function surveyResponse(
  values: Partial<InscripcionInitialSurveyResponse> = {}
): InscripcionInitialSurveyResponse {
  return { ...EMPTY_INITIAL_SURVEY_RESPONSE, ...values };
}

const RESOLVED = {
  fresh: {
    initialSurvey: surveyResponse({ encuesta: null }),
    loadFailed: false,
  } as InscripcionInitialSurveyResolved,
  enProgreso: (
    seccion: InscripcionInitialSurvey['seccionActiva']
  ): InscripcionInitialSurveyResolved => ({
    initialSurvey: surveyResponse({ encuesta: survey({ seccionActiva: seccion }) }),
    loadFailed: false,
  }),
  completa: {
    initialSurvey: surveyResponse({ encuesta: survey({ completa: true }) }),
    loadFailed: false,
  } as InscripcionInitialSurveyResolved,
  sinDerecho: {
    initialSurvey: surveyResponse({ tieneDerechoEncuesta: false, encuesta: null }),
    loadFailed: false,
  } as InscripcionInitialSurveyResolved,
  loadFailed: { initialSurvey: null, loadFailed: true } as InscripcionInitialSurveyResolved,
};

const FULL_SUMMARY: InscripcionSummary = {
  idOferta: 300,
  idProducto: 20,
  carrera: 'Sistemas',
  comienzo: 'Marzo 2027',
  turno: 'Noche',
};

function interes(idOferta: number | null): InscripcionOfertaResumen {
  return { idInscripcion: null, idOferta, nombre: 'Oferta', comienzo: null, turno: null };
}

function detail(values: Partial<InscripcionDetail> = {}): InscripcionDetail {
  return {
    estado: 'En proceso',
    detalle: null,
    intereses: [],
    pagoPendiente: null,
    seniaMinima: null,
    confirmada: null,
    ...values,
  };
}

// Los params de la URL (idProducto+idProceso) siempre están: son lo que define la
// intención de retomar. El nivel del producto llega resuelto por el resolver; `null` =
// catálogo caído o producto fuera del catálogo.
function retomar(
  detail: InscripcionDetail | null,
  idNivelProducto: number | null = null
): InscripcionEntryResolved {
  return { intent: 'retomar', detail, idProducto: 2184, idProceso: 122, idNivelProducto };
}

function reactivar(
  detail: InscripcionDetail | null,
  idNivelProducto: number | null = null
): InscripcionEntryResolved {
  return { intent: 'reactivar', detail, idProducto: 2184, idProceso: 122, idNivelProducto };
}

const DETAIL = {
  enProcesoFull: detail({
    estado: 'En proceso',
    detalle: FULL_SUMMARY,
    intereses: [interes(300)],
  }),
  // AP con varios seminarios elegidos: la precarga debe conservar todas las ofertas.
  enProcesoMultiOferta: detail({
    estado: 'En proceso',
    detalle: FULL_SUMMARY,
    intereses: [interes(310), interes(311), interes(null)],
  }),
  // Sin ofertas de interés no hay con qué reconfirmar: no hay precarga posible.
  enProcesoSinIntereses: detail({ estado: 'En proceso', detalle: FULL_SUMMARY, intereses: [] }),
  enProcesoNoDetalle: detail({ estado: 'En proceso', detalle: null, intereses: [interes(300)] }),
  enProcesoDetalleSinProducto: detail({
    estado: 'En proceso',
    detalle: { ...FULL_SUMMARY, idProducto: null },
    intereses: [interes(300)],
  }),
  pagoPendienteSinSenia: detail({
    estado: 'Pago pendiente',
    pagoPendiente: {
      idInscripcion: 1072704,
      senia: 15500,
      saldoCuenta: 1200,
      fechaVencimientoPago: '2027-03-04',
      resumen: FULL_SUMMARY,
      seminarios: [],
    },
  }),
  pagoPendienteConSenia: detail({
    estado: 'Pago pendiente',
    seniaMinima: { metodoPago: 'ABITAB', cedula: '12345678', codigoPersona: 555, senia: 3339 },
  }),
  pendienteSinSenia: detail({
    estado: 'Pendiente',
    pagoPendiente: {
      idInscripcion: 1072704,
      senia: 15500,
      saldoCuenta: 1200,
      fechaVencimientoPago: '2027-03-04',
      resumen: FULL_SUMMARY,
      seminarios: [],
    },
  }),
  pendienteConSenia: detail({
    estado: 'Pendiente',
    seniaMinima: { metodoPago: 'ABITAB', cedula: '12345678', codigoPersona: 555, senia: 3339 },
  }),
  confirmada: detail({
    estado: 'Confirmada',
    confirmada: {
      numeroEstudiante: 397654,
      resumen: FULL_SUMMARY,
      coordinadorAcademico: null,
      coordinadorCursos: null,
      inscripciones: [],
    },
  }),
  aLaEspera: detail({ estado: 'A la espera', detalle: null }),
  desconocido: detail({ estado: null, detalle: null }),
  cancelada: detail({ estado: 'Cancelada', detalle: FULL_SUMMARY }),
};

// Proyección de los campos que forman el contrato de negocio de cada escenario.
function project(state: InscripcionInitialState) {
  return {
    step: state.step,
    survey: state.survey.kind,
    activeSection: state.survey.kind === 'prefilled' ? state.survey.activeSection : undefined,
    includeAcademic:
      state.survey.kind === 'prefilled' ? state.survey.includeAcademicSelection : undefined,
    resumeInProgress: state.resumeInProgress,
    payment: state.payment.kind,
  };
}

type Row = [
  name: string,
  entry: InscripcionEntryResolved,
  survey: InscripcionInitialSurveyResolved,
  expected: ReturnType<typeof project>,
];

// --- Tabla de escenarios ----------------------------------------------------

describe('deriveInitialInscripcionState', () => {
  const rows: Row[] = [
    // Intención NUEVA: paso 1 SIEMPRE virgen; la encuesta previa nunca lo prellena.
    [
      'nueva + fresh',
      { intent: 'nueva' },
      RESOLVED.fresh,
      {
        step: 'propuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'nueva + encuesta en-progreso (BUG corregido)',
      { intent: 'nueva' },
      RESOLVED.enProgreso('experiencia-ort'),
      {
        step: 'propuesta',
        survey: 'prefilled',
        activeSection: 'experiencia-ort',
        includeAcademic: false,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'nueva + encuesta sin seccionActiva',
      { intent: 'nueva' },
      RESOLVED.enProgreso(null),
      {
        step: 'propuesta',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: false,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'nueva + encuesta completa',
      { intent: 'nueva' },
      RESOLVED.completa,
      {
        step: 'propuesta',
        survey: 'prefilled',
        activeSection: 'identidad',
        includeAcademic: false,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'nueva + sin derecho a encuesta',
      { intent: 'nueva' },
      RESOLVED.sinDerecho,
      {
        step: 'propuesta',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'nueva + encuesta con error de carga',
      { intent: 'nueva' },
      RESOLVED.loadFailed,
      {
        step: 'propuesta',
        survey: 'load-failed',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'none',
      },
    ],

    // Intención RETOMAR: el paso 1 NUNCA aparece, ni con el Detalle caído. La precarga
    // degrada a los params de la URL, así que la selección académica de la encuesta
    // nunca pisa el paso 1 (`includeAcademic: false`) y el paso 1 queda bloqueado
    // (`resumeInProgress: true`) en todos los casos.
    [
      'retomar sin detalle (Detalle falló) + en-progreso',
      retomar(null),
      RESOLVED.enProgreso('decision-academica'),
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'decision-academica',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar sin detalle (Detalle falló) + fresh',
      retomar(null),
      RESOLVED.fresh,
      {
        step: 'encuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],

    // 'En proceso' ⇒ paso 2 para cualquier nivel y con o sin encuesta previa.
    [
      'retomar En proceso + detalle full + en-progreso',
      retomar(DETAIL.enProcesoFull),
      RESOLVED.enProgreso('experiencia-ort'),
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'experiencia-ort',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + fresh',
      retomar(DETAIL.enProcesoFull),
      RESOLVED.fresh,
      {
        step: 'encuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + sin derecho',
      retomar(DETAIL.enProcesoFull),
      RESOLVED.sinDerecho,
      {
        step: 'encuesta',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle sin producto + sin derecho',
      retomar(DETAIL.enProcesoDetalleSinProducto),
      RESOLVED.sinDerecho,
      {
        step: 'encuesta',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + completa',
      retomar(DETAIL.enProcesoFull),
      RESOLVED.completa,
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'identidad',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + loadFailed',
      retomar(DETAIL.enProcesoFull),
      RESOLVED.loadFailed,
      {
        step: 'encuesta',
        survey: 'load-failed',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],

    // Sin ofertas en el Detalle igual va al paso 2 (confirmar fallará hasta que el
    // Detalle responda, pero el paso 1 no vuelve a aparecer).
    [
      'retomar En proceso + detalle sin producto + en-progreso',
      retomar(DETAIL.enProcesoDetalleSinProducto),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + sin detalle (bloque null) + en-progreso',
      retomar(DETAIL.enProcesoNoDetalle),
      RESOLVED.enProgreso('decision-academica'),
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'decision-academica',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + sin ofertas de interés + en-progreso',
      retomar(DETAIL.enProcesoSinIntereses),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],

    // Actualización profesional (nivel 3/4): mismo comportamiento que el resto. Es el
    // caso que estaba roto: AP nunca postea EncuestaInicial, así que sin encuesta
    // terminaba en el paso 1 vacío.
    [
      'retomar AP (nivel 3) En proceso + fresh',
      retomar(DETAIL.enProcesoMultiOferta, 3),
      RESOLVED.fresh,
      {
        step: 'encuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar AP (nivel 4) En proceso + sin derecho',
      retomar(DETAIL.enProcesoFull, 4),
      RESOLVED.sinDerecho,
      {
        step: 'encuesta',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar AP En proceso + encuesta por-persona en-progreso',
      retomar(DETAIL.enProcesoFull, 3),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar AP En proceso + detalle sin producto + fresh',
      retomar(DETAIL.enProcesoDetalleSinProducto, 3),
      RESOLVED.fresh,
      {
        step: 'encuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar nivel 1 En proceso + fresh',
      retomar(DETAIL.enProcesoFull, 1),
      RESOLVED.fresh,
      {
        step: 'encuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      // Catálogo caído ⇒ nivel null: igual va al paso 2; el tipo de propuesta lo
      // completa después AcademicProposalSelection desde el nivel de la carrera.
      'retomar con nivel null (catálogo caído) + fresh',
      retomar(DETAIL.enProcesoFull, null),
      RESOLVED.fresh,
      {
        step: 'encuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar AP Pago pendiente sin seña',
      retomar(DETAIL.pagoPendienteSinSenia, 3),
      RESOLVED.fresh,
      {
        step: 'pago',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'awaiting-method',
      },
    ],

    // Estados de pago / terminales: la pantalla la decide `payment` y el paso queda en
    // el 3 para que el paso 1 nunca sea el paso corriente al retomar.
    [
      'retomar Pago pendiente sin seña',
      retomar(DETAIL.pagoPendienteSinSenia),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'pago',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'awaiting-method',
      },
    ],
    [
      'retomar Pago pendiente con seña',
      retomar(DETAIL.pagoPendienteConSenia),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'pago',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'reserva',
      },
    ],
    [
      'retomar Pendiente sin seña',
      retomar(DETAIL.pendienteSinSenia),
      RESOLVED.fresh,
      {
        step: 'pago',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'awaiting-method',
      },
    ],
    [
      'retomar Pendiente con seña',
      retomar(DETAIL.pendienteConSenia),
      RESOLVED.fresh,
      {
        step: 'pago',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'reserva',
      },
    ],
    [
      'retomar Confirmada',
      retomar(DETAIL.confirmada),
      RESOLVED.completa,
      {
        step: 'pago',
        survey: 'prefilled',
        activeSection: 'identidad',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'confirmada',
      },
    ],
    [
      'retomar Confirmada + loadFailed (error+retry gana en template)',
      retomar(DETAIL.confirmada),
      RESOLVED.loadFailed,
      {
        step: 'pago',
        survey: 'load-failed',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'confirmada',
      },
    ],
    [
      'retomar A la espera',
      retomar(DETAIL.aLaEspera),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'pago',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'en-proceso',
      },
    ],
    [
      'retomar estado desconocido/null',
      retomar(DETAIL.desconocido),
      RESOLVED.fresh,
      {
        step: 'pago',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'en-proceso',
      },
    ],
    [
      'retomar Pago pendiente con seña + sin derecho',
      retomar(DETAIL.pagoPendienteConSenia),
      RESOLVED.sinDerecho,
      {
        step: 'pago',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'reserva',
      },
    ],

    // Intención REACTIVAR (provisional = nueva) hasta que negocio defina reglas.
    [
      'reactivar Cancelada + detalle full (provisional = nueva)',
      reactivar(DETAIL.cancelada),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'propuesta',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: false,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'reactivar sin detalle (Detalle falló)',
      reactivar(null),
      RESOLVED.fresh,
      {
        step: 'propuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
  ];

  it.each(rows)('%s', (_name, entry, surveyResolved, expected) => {
    expect(project(deriveInitialInscripcionState({ entry, survey: surveyResolved }))).toEqual(
      expected
    );
  });

  it('reconstructs preEnrollment (monto/vencimiento/resumen) for pending payment', () => {
    const state = deriveInitialInscripcionState({
      entry: retomar(DETAIL.pagoPendienteSinSenia),
      survey: RESOLVED.fresh,
    });
    expect(state.preEnrollment).toEqual({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: '2027-03-04',
      seniaInscripcion: 15500,
      saldoCuenta: 1200,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
      seminarios: [],
    });
  });

  it('maps the chosen deposit method for a reserva outcome', () => {
    const state = deriveInitialInscripcionState({
      entry: retomar(DETAIL.pagoPendienteConSenia),
      survey: RESOLVED.fresh,
    });
    expect(state.payment).toEqual({
      kind: 'reserva',
      method: 'abitab',
      reservation: { cedula: '12345678', codigoPersona: 555 },
    });
  });

  it('carries the confirmed detail for a confirmada outcome', () => {
    const state = deriveInitialInscripcionState({
      entry: retomar(DETAIL.confirmada),
      survey: RESOLVED.completa,
    });
    expect(state.payment).toEqual({ kind: 'confirmada', detail: DETAIL.confirmada.confirmada });
  });

  it('builds the academic prefill from every interest offering when resuming an AP', () => {
    const state = deriveInitialInscripcionState({
      entry: retomar(DETAIL.enProcesoMultiOferta, 3),
      survey: RESOLVED.fresh,
    });
    // `turno` y `seminarios` se llenan los dos: el payload de confirmación lee uno u
    // otro según el tipo de propuesta.
    expect(state.academicPrefill).toEqual({
      tipoPropuesta: '3',
      carrera: '20',
      comienzo: '122',
      turno: '310',
      seminarios: ['310', '311'],
    });
  });

  it('prefills the academic step for a non-AP resume too', () => {
    const state = deriveInitialInscripcionState({
      entry: retomar(DETAIL.enProcesoFull, 1),
      survey: RESOLVED.enProgreso('educacion'),
    });
    expect(state.academicPrefill).toEqual({
      tipoPropuesta: '1',
      carrera: '20',
      comienzo: '122',
      turno: '300',
      seminarios: ['300'],
    });
  });

  it('leaves the proposal type empty when the career catalog failed', () => {
    const state = deriveInitialInscripcionState({
      entry: retomar(DETAIL.enProcesoFull, null),
      survey: RESOLVED.fresh,
    });
    expect(state.academicPrefill).toEqual({
      tipoPropuesta: '',
      carrera: '20',
      comienzo: '122',
      turno: '300',
      seminarios: ['300'],
    });
  });

  // Sin Detalle (o sin su bloque de producto) la precarga degrada a los params de la
  // URL: son la prueba de que la inscripción existe.
  it.each([
    ['sin Detalle', null],
    ['con Detalle sin bloque de producto', DETAIL.enProcesoNoDetalle],
  ])('prefills the academic step from the URL params %s', (_name, detail) => {
    const state = deriveInitialInscripcionState({
      entry: retomar(detail),
      survey: RESOLVED.fresh,
    });
    expect(state.academicPrefill).toEqual({
      tipoPropuesta: '',
      carrera: '2184',
      comienzo: '122',
      turno: detail ? '300' : '',
      seminarios: detail ? ['300'] : [],
    });
  });

  it('never prefills nor locks step 1 on a new inscription', () => {
    const nueva = deriveInitialInscripcionState({
      entry: { intent: 'nueva' },
      survey: RESOLVED.fresh,
    });
    expect(nueva.academicPrefill).toBeNull();
    expect(nueva.resumeInProgress).toBe(false);
    expect(nueva.step).toBe('propuesta');
  });

  // Invariante duro: con idProducto+idProceso en la URL la inscripción existe, así que
  // ninguna combinación de estado/encuesta puede aterrizar en el paso 1.
  it('never lands on step 1 when resuming, whatever the detail and survey are', () => {
    const details = [null, ...Object.values(DETAIL)];
    const surveys = Object.values(RESOLVED).map(resolved =>
      typeof resolved === 'function' ? resolved('educacion') : resolved
    );

    for (const detail of details) {
      for (const survey of surveys) {
        for (const idNivelProducto of [null, 1, 3]) {
          const state = deriveInitialInscripcionState({
            entry: retomar(detail, idNivelProducto),
            survey,
          });
          expect(state.step).not.toBe('propuesta');
          expect(state.resumeInProgress).toBe(true);
          expect(state.academicPrefill).not.toBeNull();
        }
      }
    }
  });

  it('computes completed sections up to the active one', () => {
    const state = deriveInitialInscripcionState({
      entry: retomar(DETAIL.enProcesoFull),
      survey: RESOLVED.enProgreso('experiencia-ort'),
    });
    expect(state.survey.kind === 'prefilled' && state.survey.completedSections).toEqual([
      'educacion',
      'decision-academica',
    ]);
  });
});
