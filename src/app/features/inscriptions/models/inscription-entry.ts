import { getAcademicProposalTypeByLevel } from '../../catalogs/models/academic-proposal';
import {
  detailToPreEnrollment,
  type InscripcionConfirmedDetail,
  type InscripcionDetail,
} from './inscription-detail';
import type {
  InscripcionInitialSurveyResponse,
  InscripcionPreEnrollmentResponse,
  InscripcionReservationData,
  MetodoPago,
  SeccionEncuestaId,
} from './inscription-flow';
import { fromApiPaymentMethod } from './inscription-flow-mappers';
import { getSeccionesVisibles } from './inscription-flow-policy';
import type { InscripcionStep } from './inscription-process';

/**
 * Resultado del resolver de encuesta inicial. Vive acá (capa de modelos) para que
 * tanto el resolver como la derivación de estado dependan del mismo contrato sin
 * invertir la dirección de dependencias.
 */
export interface InscripcionInitialSurveyResolved {
  initialSurvey: InscripcionInitialSurveyResponse | null;
  loadFailed: boolean;
}

/** Respuesta de encuesta "vacía pero con derecho": persona sin encuesta previa. */
export const EMPTY_INITIAL_SURVEY_RESPONSE: InscripcionInitialSurveyResponse = {
  tieneDerechoEncuesta: true,
  encuesta: null,
  universidadesConsideradas: [],
  universidadesConsideradasOtros: [],
  universidadesEducacionSuperior: [],
  universidadesEducacionSuperiorOtros: [],
  opcionesMotivosSeleccionados: [],
  opcionesPublicidadSeleccionadas: [],
};

/**
 * Intención de entrada al flujo, decidida UNA vez en la ruta a partir de la URL,
 * nunca inferida del estado del backend. Es la lección del bug que originó este
 * módulo: sin intención explícita, una encuesta en progreso (por persona) hacía
 * que "empezar de cero" precargara el paso 1.
 *
 * - `nueva`: el usuario empieza una inscripción. Paso 1 SIEMPRE virgen y editable.
 * - `retomar`: continúa una inscripción existente. Llegar con idProducto+idProceso
 *   válidos YA significa que la inscripción existe, así que el paso 1 nunca se muestra,
 *   ni siquiera con `detail === null` (Detalle caído): la precarga degrada a los params
 *   de la URL y el flujo arranca igual en el paso 2.
 * - `reactivar`: el POST ya creó la nueva inscripción. Su respuesta evita volver a
 *   consultar Detalle; si no está disponible, Detalle conserva el fallback.
 */
export type InscripcionEntryResolved =
  | { intent: 'nueva' }
  | {
      intent: 'retomar';
      detail: InscripcionDetail | null;
      idProducto: number;
      idProceso: number;
      idOfertas: number[];
      idNivelProducto: number | null;
    }
  | {
      intent: 'reactivar';
      detail: InscripcionDetail | null;
      preEnrollment: InscripcionPreEnrollmentResponse | null;
      idProducto: number;
      idProceso: number;
      idOfertas: number[];
      idNivelProducto: number | null;
    };

/** Entrada con inscripción existente: la URL trae idProducto+idProceso válidos. */
export type InscripcionResumeEntry = Extract<
  InscripcionEntryResolved,
  { intent: 'retomar' | 'reactivar' }
>;

export type InscripcionEntryIntent = InscripcionEntryResolved['intent'];

export interface InscripcionEntryContext {
  entry: InscripcionEntryResolved;
  survey: InscripcionInitialSurveyResolved;
}

export type InscripcionSurveyInit =
  | { kind: 'load-failed' }
  | { kind: 'identity-only' }
  | { kind: 'fresh' }
  | {
      kind: 'prefilled';
      response: InscripcionInitialSurveyResponse;
      surveyState: 'en-progreso' | 'completa';
      activeSection: SeccionEncuestaId;
      completedSections: readonly SeccionEncuestaId[];
      /** En una inscripción nueva, la encuesta previa nunca pisa el Paso 1. */
      includeAcademicSelection: boolean;
    };

export type InscripcionPaymentInit =
  | { kind: 'none' }
  | { kind: 'awaiting-method' }
  | { kind: 'reserva'; method: MetodoPago | null; reservation: InscripcionReservationData | null }
  | { kind: 'confirmada'; detail: InscripcionConfirmedDetail | null }
  | { kind: 'en-proceso' };

/**
 * Precarga del paso 1 al retomar. La tarjeta transporta las ofertas en la URL y el
 * Detalle queda como fallback para enlaces anteriores; la encuesta nunca es fuente del
 * paso 1. Si el Detalle no llegó, producto y comienzo también salen de la URL.
 *
 * `turno` y `seminarios` se llenan SIEMPRE los dos con las mismas ofertas:
 * `buildConfirmPreEnrollmentPayload` lee `seminarios` cuando el tipo es Actualización
 * profesional y `turno` en el resto, y el tipo puede quedar vacío si el catálogo de
 * carreras falló. Llenando ambos, confirmar la preinscripción funciona igual.
 */
export interface InscripcionAcademicPrefill {
  tipoPropuesta: string;
  carrera: string;
  /** El control `comienzo` guarda un idProceso, que es el param de la URL. */
  comienzo: string;
  turno: string;
  seminarios: string[];
}

export interface InscripcionInitialState {
  step: InscripcionStep;
  survey: InscripcionSurveyInit;
  payment: InscripcionPaymentInit;
  resumeInProgress: boolean;
  preEnrollment: InscripcionPreEnrollmentResponse | null;
  academicPrefill: InscripcionAcademicPrefill | null;
}

/**
 * Deriva TODO el estado inicial del flujo a partir del contexto de entrada. Función
 * pura y sin efectos: es la única fuente de verdad de "en qué estado arranca la
 * inscripción". El backend (Detalle + EncuestaInicial) manda sobre los datos; la
 * intención manda sobre presentación/navegación. Su tabla de escenarios
 * (`inscription-entry.spec.ts`) es el contrato ejecutable.
 */
export function deriveInitialInscripcionState(
  ctx: InscripcionEntryContext
): InscripcionInitialState {
  switch (ctx.entry.intent) {
    case 'nueva':
      return deriveNueva(ctx);
    case 'reactivar':
      return deriveReactivar(ctx, ctx.entry);
    case 'retomar':
      return deriveRetomar(ctx, ctx.entry);
  }
}

function deriveReactivar(
  ctx: InscripcionEntryContext,
  entry: Extract<InscripcionEntryResolved, { intent: 'reactivar' }>
): InscripcionInitialState {
  const response = entry.preEnrollment;
  if (!response) return deriveRetomar(ctx, entry);

  return {
    step: 'pago',
    survey: deriveSurvey(ctx, false),
    payment:
      response.enEspera === true
        ? { kind: 'en-proceso' }
        : response.seniaInscripcion === 0
          ? { kind: 'reserva', method: null, reservation: null }
          : { kind: 'awaiting-method' },
    resumeInProgress: true,
    preEnrollment: response,
    academicPrefill: buildAcademicPrefill(entry),
  };
}

function deriveNueva(ctx: InscripcionEntryContext): InscripcionInitialState {
  return {
    step: 'propuesta',
    survey: deriveSurvey(ctx, false),
    payment: { kind: 'none' },
    resumeInProgress: false,
    preEnrollment: null,
    academicPrefill: null,
  };
}

/**
 * Retomar NUNCA muestra el paso 1. Llegar con idProducto+idProceso válidos ya significa
 * que la inscripción existe: el interés está registrado y el paso 1 queda precargado
 * (desde el Detalle, o desde los params de la URL si el Detalle no llegó) y bloqueado.
 * El paso solo distingue en qué pantalla del proceso cae la inscripción; los estados
 * terminales pintan su pantalla por `payment` y quedan en el paso 3 para que el paso 1
 * no sea el paso corriente en ningún caso.
 */
function deriveRetomar(
  ctx: InscripcionEntryContext,
  entry: InscripcionResumeEntry
): InscripcionInitialState {
  const detail = entry.detail;
  const base = {
    // Con precarga del Detalle/URL, la selección académica de una encuesta previa no
    // debe pisar el paso 1 ni sobreescribir el turno con su carga asíncrona de catálogos.
    survey: deriveSurvey(ctx, false),
    resumeInProgress: true,
    preEnrollment: detail ? detailToPreEnrollment(detail) : null,
    academicPrefill: buildAcademicPrefill(entry),
  } satisfies Partial<InscripcionInitialState>;

  // Detalle caído o inconsistente: seguimos en el paso 2 con lo que aporta la URL.
  if (!detail) return { ...base, step: 'encuesta', payment: { kind: 'none' } };

  switch (detail.estado) {
    case 'Pago pendiente':
    case 'Pendiente':
      return detail.seniaMinima
        ? {
            ...base,
            step: 'pago',
            payment: {
              kind: 'reserva',
              method: fromApiPaymentMethod(detail.seniaMinima.metodoPago),
              reservation: {
                cedula: detail.seniaMinima.cedula,
                codigoPersona: detail.seniaMinima.codigoPersona,
              },
            },
          }
        : { ...base, step: 'pago', payment: { kind: 'awaiting-method' } };
    case 'Confirmada':
      return { ...base, step: 'pago', payment: { kind: 'confirmada', detail: detail.confirmada } };
    case 'En proceso':
      return { ...base, step: 'encuesta', payment: { kind: 'none' } };
    default:
      // 'A la espera', 'Cancelada', null y cualquier estado desconocido: pantalla
      // terminal informativa. ('Cancelada' quedará reservada para `reactivar`.)
      return { ...base, step: 'pago', payment: { kind: 'en-proceso' } };
  }
}

/**
 * Precarga del paso 1 al retomar. Producto y comienzo prefieren el Detalle; las ofertas
 * prefieren la URL de la tarjeta y usan `Detalle.intereses` como fallback para enlaces
 * anteriores. Con `idNivelProducto` desconocido (catálogo caído) el tipo queda vacío
 * y `AcademicProposalSelection` lo completa desde el nivel de la carrera al cargar.
 */
function buildAcademicPrefill(entry: InscripcionResumeEntry): InscripcionAcademicPrefill {
  const idProducto = entry.detail?.detalle?.idProducto ?? entry.idProducto;
  const detailOfferIds = (entry.detail?.intereses ?? [])
    .map(oferta => oferta.idOferta)
    .filter((idOferta): idOferta is number => idOferta !== null);
  const idOfertas = entry.idOfertas.length > 0 ? entry.idOfertas : detailOfferIds;

  return {
    tipoPropuesta:
      entry.idNivelProducto === null
        ? ''
        : (getAcademicProposalTypeByLevel(entry.idNivelProducto)?.value ?? ''),
    carrera: String(idProducto),
    comienzo: String(entry.idProceso),
    turno: idOfertas.length > 0 ? String(idOfertas[0]) : '',
    seminarios: idOfertas.map(String),
  };
}

function deriveSurvey(
  ctx: InscripcionEntryContext,
  includeAcademicSelection: boolean
): InscripcionSurveyInit {
  if (ctx.survey.loadFailed) return { kind: 'load-failed' };

  const response = ctx.survey.initialSurvey ?? EMPTY_INITIAL_SURVEY_RESPONSE;
  if (response.tieneDerechoEncuesta === false) return { kind: 'identity-only' };

  const encuesta = response.encuesta;
  if (!encuesta) return { kind: 'fresh' };

  const isComplete = encuesta.completa;
  const activeSection: SeccionEncuestaId = isComplete
    ? 'identidad'
    : (encuesta.seccionActiva ?? 'educacion');
  const visible = getSeccionesVisibles(isComplete ? 'encuesta-completa' : 'primera-vez');
  const activeIndex = visible.indexOf(activeSection);

  return {
    kind: 'prefilled',
    response,
    surveyState: isComplete ? 'completa' : 'en-progreso',
    activeSection,
    completedSections: activeIndex > 0 ? visible.slice(0, activeIndex) : [],
    includeAcademicSelection,
  };
}
