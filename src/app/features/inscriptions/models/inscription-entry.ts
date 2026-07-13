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
 * - `retomar`: continúa una inscripción existente (llega con idProducto+idProceso).
 *   `detail === null` ⇒ el Detalle falló y se degrada a comportamiento `nueva`.
 * - `reactivar`: reservado para el futuro botón "Reactivar" de una inscripción
 *   cancelada. Reglas de negocio TBD (ver `deriveInitialInscripcionState`).
 */
export type InscripcionEntryResolved =
  | { intent: 'nueva' }
  | { intent: 'retomar'; detail: InscripcionDetail | null }
  | { intent: 'reactivar'; detail: InscripcionDetail | null };

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

export interface InscripcionInitialState {
  step: InscripcionStep;
  survey: InscripcionSurveyInit;
  payment: InscripcionPaymentInit;
  resumeInProgress: boolean;
  preEnrollment: InscripcionPreEnrollmentResponse | null;
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
      // ponytail: sin reglas de negocio definidas, `reactivar` deriva igual que
      // `nueva` (paso 1 virgen, sin POST saltado, sin precarga). Es la opción
      // segura hasta que exista el botón y negocio defina el comportamiento; el
      // `detail` queda disponible en el contexto para esa derivación futura.
      return deriveNueva(ctx);
    case 'retomar':
      return deriveRetomar(ctx, ctx.entry.detail);
  }
}

function deriveNueva(ctx: InscripcionEntryContext): InscripcionInitialState {
  return {
    step: 'propuesta',
    survey: deriveSurvey(ctx, false),
    payment: { kind: 'none' },
    resumeInProgress: false,
    preEnrollment: null,
  };
}

function deriveRetomar(
  ctx: InscripcionEntryContext,
  detail: InscripcionDetail | null
): InscripcionInitialState {
  // Detalle falló (params inválidos o error de red) ⇒ degradar a nueva.
  if (!detail) return deriveNueva(ctx);

  const survey = deriveSurvey(ctx, true);
  const product = detail.detalle;
  const base = {
    survey,
    resumeInProgress:
      detail.estado === 'En proceso' &&
      survey.kind === 'prefilled' &&
      product !== null &&
      product.idProducto !== null &&
      product.idComienzo !== null &&
      product.idOferta !== null,
    preEnrollment: detailToPreEnrollment(detail),
  } satisfies Partial<InscripcionInitialState>;

  switch (detail.estado) {
    case 'Pago pendiente':
    case 'Pendiente':
      return detail.seniaMinima
        ? {
            ...base,
            step: 'propuesta',
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
      return {
        ...base,
        step: 'propuesta',
        payment: { kind: 'confirmada', detail: detail.confirmada },
      };
    case 'En proceso':
      return {
        ...base,
        step: survey.kind === 'prefilled' ? 'encuesta' : 'propuesta',
        payment: { kind: 'none' },
      };
    default:
      // 'A la espera', 'Cancelada', null y cualquier estado desconocido: pantalla
      // terminal informativa. ('Cancelada' quedará reservada para `reactivar`.)
      return { ...base, step: 'propuesta', payment: { kind: 'en-proceso' } };
  }
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
