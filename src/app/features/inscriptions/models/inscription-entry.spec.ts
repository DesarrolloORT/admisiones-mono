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
  idComienzo: 200,
  comienzo: 'Marzo 2027',
  idTurno: 10,
  turno: 'Noche',
};

function detail(values: Partial<InscripcionDetail> = {}): InscripcionDetail {
  return {
    estado: 'En proceso',
    detalle: null,
    pagoPendiente: null,
    seniaMinima: null,
    confirmada: null,
    ...values,
  };
}

// El nivel del producto llega resuelto por el resolver; `null` = catálogo caído o
// producto fuera del catálogo (se deriva como hasta ahora).
function retomar(
  detail: InscripcionDetail | null,
  idNivelProducto: number | null = null
): InscripcionEntryResolved {
  return { intent: 'retomar', detail, idNivelProducto };
}

function reactivar(
  detail: InscripcionDetail | null,
  idNivelProducto: number | null = null
): InscripcionEntryResolved {
  return { intent: 'reactivar', detail, idNivelProducto };
}

const DETAIL = {
  enProcesoFull: detail({ estado: 'En proceso', detalle: FULL_SUMMARY }),
  enProcesoNoDetalle: detail({ estado: 'En proceso', detalle: null }),
  enProcesoDetalleSinProducto: detail({
    estado: 'En proceso',
    detalle: { ...FULL_SUMMARY, idProducto: null },
  }),
  pagoPendienteSinSenia: detail({
    estado: 'Pago pendiente',
    pagoPendiente: {
      idInscripcion: 1072704,
      senia: 15500,
      saldoCuenta: 1200,
      fechaVencimientoPago: '2027-03-04',
      resumen: FULL_SUMMARY,
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
      materiasPrimerSemestre: [],
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

    // Intención RETOMAR degradada (Detalle falló) ⇒ se comporta como nueva.
    [
      'retomar sin detalle (Detalle falló) + en-progreso',
      retomar(null),
      RESOLVED.enProgreso('decision-academica'),
      {
        step: 'propuesta',
        survey: 'prefilled',
        activeSection: 'decision-academica',
        includeAcademic: false,
        resumeInProgress: false,
        payment: 'none',
      },
    ],

    // Intención RETOMAR con detalle: comportamiento "continuar" preservado.
    [
      'retomar En proceso + detalle full + en-progreso',
      retomar(DETAIL.enProcesoFull),
      RESOLVED.enProgreso('situacion-laboral'),
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'situacion-laboral',
        includeAcademic: true,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + fresh',
      retomar(DETAIL.enProcesoFull),
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
      // La excepción solo aplica cuando existe una encuesta para precargar el Paso 1.
      'retomar En proceso + detalle full + sin derecho',
      retomar(DETAIL.enProcesoFull),
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
      // Sin encuesta para precargar, el Paso 1 sigue editable.
      'retomar En proceso + detalle sin producto + sin derecho',
      retomar(DETAIL.enProcesoDetalleSinProducto),
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
      'retomar En proceso + detalle full + completa',
      retomar(DETAIL.enProcesoFull),
      RESOLVED.completa,
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'identidad',
        includeAcademic: true,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + loadFailed',
      retomar(DETAIL.enProcesoFull),
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

    // Una oferta incompleta no activa la excepción de reanudación.
    [
      'retomar En proceso + detalle sin producto + en-progreso',
      retomar(DETAIL.enProcesoDetalleSinProducto),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'encuesta',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: true,
        resumeInProgress: false,
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
        includeAcademic: true,
        resumeInProgress: false,
        payment: 'none',
      },
    ],

    // Actualización profesional (nivel 3/4): retoma en paso 2 SIN exigir encuesta;
    // el paso 1 se precarga desde el Detalle (ver its de academicPrefill).
    [
      'retomar AP (nivel 3) En proceso + fresh',
      retomar(DETAIL.enProcesoFull, 3),
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
        includeAcademic: true,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      // Sin oferta completa no hay nada que retomar: paso 1 editable.
      'retomar AP En proceso + detalle sin producto + fresh',
      retomar(DETAIL.enProcesoDetalleSinProducto, 3),
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
      // Guard de regresión: nivel 1 conserva el comportamiento actual exacto.
      'retomar nivel 1 En proceso + fresh (sin cambios)',
      retomar(DETAIL.enProcesoFull, 1),
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
      // Catálogo caído ⇒ nivel null ⇒ AP degrada al comportamiento actual.
      'retomar AP con nivel null (catálogo caído) + fresh',
      retomar(DETAIL.enProcesoFull, null),
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
      // La rama AP aplica solo a 'En proceso'; los estados de pago no cambian.
      'retomar AP Pago pendiente sin seña',
      retomar(DETAIL.pagoPendienteSinSenia, 3),
      RESOLVED.fresh,
      {
        step: 'pago',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'awaiting-method',
      },
    ],

    // Estados de pago / terminales.
    [
      'retomar Pago pendiente sin seña',
      retomar(DETAIL.pagoPendienteSinSenia),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'pago',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: true,
        resumeInProgress: false,
        payment: 'awaiting-method',
      },
    ],
    [
      'retomar Pago pendiente con seña',
      retomar(DETAIL.pagoPendienteConSenia),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'propuesta',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: true,
        resumeInProgress: false,
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
        resumeInProgress: false,
        payment: 'awaiting-method',
      },
    ],
    [
      'retomar Pendiente con seña',
      retomar(DETAIL.pendienteConSenia),
      RESOLVED.fresh,
      {
        step: 'propuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'reserva',
      },
    ],
    // Confirmada no trae bloque `detalle` (usa `confirmada.resumen`); includeAcademic
    // queda `true` pero es inocuo: el outcome terminal oculta paso 1/2.
    [
      'retomar Confirmada',
      retomar(DETAIL.confirmada),
      RESOLVED.completa,
      {
        step: 'propuesta',
        survey: 'prefilled',
        activeSection: 'identidad',
        includeAcademic: true,
        resumeInProgress: false,
        payment: 'confirmada',
      },
    ],
    [
      'retomar Confirmada + loadFailed (error+retry gana en template)',
      retomar(DETAIL.confirmada),
      RESOLVED.loadFailed,
      {
        step: 'propuesta',
        survey: 'load-failed',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'confirmada',
      },
    ],
    [
      'retomar A la espera',
      retomar(DETAIL.aLaEspera),
      RESOLVED.enProgreso('educacion'),
      {
        step: 'propuesta',
        survey: 'prefilled',
        activeSection: 'educacion',
        includeAcademic: true,
        resumeInProgress: false,
        payment: 'en-proceso',
      },
    ],
    [
      'retomar estado desconocido/null',
      retomar(DETAIL.desconocido),
      RESOLVED.fresh,
      {
        step: 'propuesta',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'en-proceso',
      },
    ],
    [
      'retomar Pago pendiente con seña + sin derecho',
      retomar(DETAIL.pagoPendienteConSenia),
      RESOLVED.sinDerecho,
      {
        step: 'propuesta',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
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

  it('builds the academic prefill from the detail when resuming an AP inscription', () => {
    const state = deriveInitialInscripcionState({
      entry: retomar(DETAIL.enProcesoFull, 3),
      survey: RESOLVED.fresh,
    });
    expect(state.academicPrefill).toEqual({
      tipoPropuesta: '3',
      carrera: '20',
      seminarios: ['300'],
    });
  });

  it('does not prefill the academic step outside the AP resume scenario', () => {
    const nivel1 = deriveInitialInscripcionState({
      entry: retomar(DETAIL.enProcesoFull, 1),
      survey: RESOLVED.enProgreso('educacion'),
    });
    const nueva = deriveInitialInscripcionState({
      entry: { intent: 'nueva' },
      survey: RESOLVED.fresh,
    });
    expect(nivel1.academicPrefill).toBeNull();
    expect(nueva.academicPrefill).toBeNull();
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
