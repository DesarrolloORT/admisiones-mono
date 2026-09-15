import { getAcademicProposalTypeByLevel } from '../../catalogs/models/academic-proposal';
import {
  detailToPreEnrollment,
  type EnrollmentConfirmedDetail,
  type EnrollmentDetail,
} from './enrollment-detail';
import type {
  EnrollmentInitialSurveyResponse,
  EnrollmentPreEnrollmentResponse,
  EnrollmentReservationData,
  PaymentMethod,
  SurveySectionId,
} from './enrollment-flow';
import { fromApiPaymentMethod } from './enrollment-flow-mappers';
import { getVisibleSections } from './enrollment-flow-policy';
import type { EnrollmentStep } from './enrollment-process';

/**
 * Resultado del resolver de encuesta inicial. Vive acá (capa de modelos) para que
 * tanto el resolver como la derivación de estado dependan del mismo contrato sin
 * invertir la dirección de dependencias.
 */
export interface EnrollmentInitialSurveyResolved {
  initialSurvey: EnrollmentInitialSurveyResponse | null;
  loadFailed: boolean;
  loadFailedMessage?: string | null;
}

/** Respuesta de encuesta "vacía pero con derecho": persona sin encuesta previa. */
export const EMPTY_INITIAL_SURVEY_RESPONSE: EnrollmentInitialSurveyResponse = {
  isEligibleForSurvey: true,
  survey: null,
  consideredUniversities: [],
  otherConsideredUniversities: [],
  higherEducationUniversities: [],
  otherHigherEducationUniversities: [],
  selectedReasonOptions: [],
  selectedAdvertisingOptions: [],
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
export type EnrollmentEntryResolved =
  | { intent: 'new' }
  | {
      intent: 'resume';
      detail: EnrollmentDetail | null;
      productId: number;
      admissionProcessId: number;
      offeringIds: number[];
      productLevelId: number | null;
    }
  | {
      intent: 'reactivate';
      detail: EnrollmentDetail | null;
      preEnrollment: EnrollmentPreEnrollmentResponse | null;
      productId: number;
      admissionProcessId: number;
      offeringIds: number[];
      productLevelId: number | null;
    };

/** Entrada con inscripción existente: la URL trae idProducto+idProceso válidos. */
export type EnrollmentResumeEntry = Extract<
  EnrollmentEntryResolved,
  { intent: 'resume' | 'reactivate' }
>;

export type EnrollmentEntryIntent = EnrollmentEntryResolved['intent'];

export interface EnrollmentEntryContext {
  entry: EnrollmentEntryResolved;
  survey: EnrollmentInitialSurveyResolved;
}

export const DEFAULT_SURVEY_LOAD_FAILED_MESSAGE =
  'No se pudo consultar el estado de tu encuesta. Intentá nuevamente.';

export type EnrollmentSurveyInit =
  | { kind: 'load-failed'; message: string }
  | { kind: 'identity-only' }
  | { kind: 'fresh' }
  | {
      kind: 'prefilled';
      response: EnrollmentInitialSurveyResponse;
      activeSection: SurveySectionId;
      completedSections: readonly SurveySectionId[];
      /** En una inscripción nueva, la encuesta previa nunca pisa el Paso 1. */
      includeAcademicSelection: boolean;
    };

export type EnrollmentPaymentInit =
  | { kind: 'none' }
  | { kind: 'awaiting-method' }
  | {
      kind: 'reservation';
      method: PaymentMethod | null;
      reservation: EnrollmentReservationData | null;
    }
  | { kind: 'confirmed'; detail: EnrollmentConfirmedDetail | null }
  | { kind: 'in-progress' };

/**
 * Precarga del paso 1 al retomar. La tarjeta transporta las ofertas en la URL y el
 * Detalle queda como fallback para enlaces anteriores; la encuesta nunca es fuente del
 * paso 1. Si el Detalle no llegó, producto y comienzo también salen de la URL.
 *
 * `shift` y `seminars` se llenan SIEMPRE los dos con las mismas ofertas:
 * `buildConfirmPreEnrollmentPayload` lee `seminars` cuando el tipo es Actualización
 * profesional y `shift` en el resto, y el tipo puede quedar vacío si el catálogo de
 * carreras falló. Llenando ambos, confirmar la preinscripción funciona igual.
 */
export interface EnrollmentAcademicPrefill {
  proposalType: string;
  degreeProgram: string;
  intake: string;
  shift: string;
  seminars: string[];
}

export interface EnrollmentInitialState {
  step: EnrollmentStep;
  survey: EnrollmentSurveyInit;
  payment: EnrollmentPaymentInit;
  resumeInProgress: boolean;
  preEnrollment: EnrollmentPreEnrollmentResponse | null;
  academicPrefill: EnrollmentAcademicPrefill | null;
}

/**
 * Deriva TODO el estado inicial del flujo a partir del contexto de entrada. Función
 * pura y sin efectos: es la única fuente de verdad de "en qué estado arranca la
 * inscripción". El backend (detalle + encuesta inicial) manda sobre los datos; la
 * intención manda sobre presentación/navegación. Su tabla de escenarios
 * (`enrollment-entry.spec.ts`) es el contrato ejecutable.
 */
export function deriveInitialEnrollmentState(ctx: EnrollmentEntryContext): EnrollmentInitialState {
  switch (ctx.entry.intent) {
    case 'new':
      return deriveNew(ctx);
    case 'reactivate':
      return deriveReactivate(ctx, ctx.entry);
    case 'resume':
      return deriveResume(ctx, ctx.entry);
  }
}

function deriveReactivate(
  ctx: EnrollmentEntryContext,
  entry: Extract<EnrollmentEntryResolved, { intent: 'reactivate' }>
): EnrollmentInitialState {
  const response = entry.preEnrollment;
  if (!response) return deriveResume(ctx, entry);

  return {
    step: 'payment',
    survey: deriveSurvey(ctx, false),
    payment:
      response.isWaiting === true
        ? { kind: 'in-progress' }
        : response.enrollmentDeposit === 0
          ? { kind: 'reservation', method: null, reservation: null }
          : { kind: 'awaiting-method' },
    resumeInProgress: true,
    preEnrollment: response,
    academicPrefill: buildAcademicPrefill(entry),
  };
}

function deriveNew(ctx: EnrollmentEntryContext): EnrollmentInitialState {
  return {
    step: 'proposal',
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
function deriveResume(
  ctx: EnrollmentEntryContext,
  entry: EnrollmentResumeEntry
): EnrollmentInitialState {
  const detail = entry.detail;
  const base = {
    // Con precarga del Detalle/URL, la selección académica de una encuesta previa no
    // debe pisar el paso 1 ni sobreescribir el turno con su carga asíncrona de catálogos.
    survey: deriveSurvey(ctx, false),
    resumeInProgress: true,
    preEnrollment: detail ? detailToPreEnrollment(detail) : null,
    academicPrefill: buildAcademicPrefill(entry),
  } satisfies Partial<EnrollmentInitialState>;

  // Detalle caído o inconsistente: seguimos en el paso 2 con lo que aporta la URL.
  if (!detail) return { ...base, step: 'survey', payment: { kind: 'none' } };

  switch (detail.status) {
    case 'Pago pendiente':
    case 'Pendiente':
      return detail.minimumDeposit
        ? {
            ...base,
            step: 'payment',
            payment: {
              kind: 'reservation',
              method: fromApiPaymentMethod(detail.minimumDeposit.paymentMethod),
              reservation: {
                documentNumber: detail.minimumDeposit.documentNumber,
                personCode: detail.minimumDeposit.personCode,
              },
            },
          }
        : { ...base, step: 'payment', payment: { kind: 'awaiting-method' } };
    case 'Confirmada':
      return { ...base, step: 'payment', payment: { kind: 'confirmed', detail: detail.confirmed } };
    case 'En proceso':
      return { ...base, step: 'survey', payment: { kind: 'none' } };
    default:
      // 'A la espera', 'Cancelada', null y cualquier estado desconocido: pantalla
      // terminal informativa. ('Cancelada' quedará reservada para `reactivar`.)
      return { ...base, step: 'payment', payment: { kind: 'in-progress' } };
  }
}

/**
 * Precarga del paso 1 al retomar. Producto y comienzo prefieren el Detalle; las ofertas
 * prefieren la URL de la tarjeta y usan `detail.interests` como fallback para enlaces
 * anteriores. Con `productLevelId` desconocido (catálogo caído) el tipo queda vacío
 * y `AcademicProposalSelection` lo completa desde el nivel de la carrera al cargar.
 */
function buildAcademicPrefill(entry: EnrollmentResumeEntry): EnrollmentAcademicPrefill {
  const productId = entry.detail?.summary?.productId ?? entry.productId;
  const detailOfferIds = (entry.detail?.interests ?? [])
    .map(offering => offering.offeringId)
    .filter((offeringId): offeringId is number => offeringId !== null);
  const offeringIds = entry.offeringIds.length > 0 ? entry.offeringIds : detailOfferIds;

  return {
    proposalType:
      entry.productLevelId === null
        ? ''
        : (getAcademicProposalTypeByLevel(entry.productLevelId)?.value ?? ''),
    degreeProgram: String(productId),
    intake: String(entry.admissionProcessId),
    shift: offeringIds.length > 0 ? String(offeringIds[0]) : '',
    seminars: offeringIds.map(String),
  };
}

function deriveSurvey(
  ctx: EnrollmentEntryContext,
  includeAcademicSelection: boolean
): EnrollmentSurveyInit {
  if (ctx.survey.loadFailed) {
    return {
      kind: 'load-failed',
      message: ctx.survey.loadFailedMessage ?? DEFAULT_SURVEY_LOAD_FAILED_MESSAGE,
    };
  }

  const response = ctx.survey.initialSurvey ?? EMPTY_INITIAL_SURVEY_RESPONSE;
  if (response.isEligibleForSurvey === false) return { kind: 'identity-only' };

  const survey = response.survey;
  if (!survey) return { kind: 'fresh' };

  // `canAnswerSurvey` manda sobre el estado de la encuesta: con derecho a responderla, los
  // campos se muestran editables con lo ya respondido precargado, aunque el backend la
  // marque completa. Cuando no hay derecho, la rama `identity-only` ya cortó más arriba.
  const activeSection: SurveySectionId = survey.activeSection ?? 'education';
  const visible = getVisibleSections(true);
  const activeIndex = visible.indexOf(activeSection);

  return {
    kind: 'prefilled',
    response,
    activeSection,
    completedSections: activeIndex > 0 ? visible.slice(0, activeIndex) : [],
    includeAcademicSelection,
  };
}
