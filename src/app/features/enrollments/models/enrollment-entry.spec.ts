import type { EnrollmentDetail, EnrollmentSummary } from './enrollment-detail';
import {
  deriveInitialEnrollmentState,
  EMPTY_INITIAL_SURVEY_RESPONSE,
  type EnrollmentEntryResolved,
  type EnrollmentInitialState,
  type EnrollmentInitialSurveyResolved,
} from './enrollment-entry';
import type {
  EnrollmentInitialSurvey,
  EnrollmentInitialSurveyResponse,
  EnrollmentOfferingSummary,
  EnrollmentPreEnrollmentResponse,
} from './enrollment-flow';

// --- Fixtures ---------------------------------------------------------------

function survey(values: Partial<EnrollmentInitialSurvey> = {}): EnrollmentInitialSurvey {
  return {
    degreeProgramId: null,
    intakeId: null,
    shiftId: null,
    productLevelId: null,
    complete: false,
    activeSection: null,
    studiesHighSchool: null,
    highSchoolOrientationId: null,
    highSchoolYearId: null,
    repeatsHighSchoolYear: null,
    highSchoolYearRepeatCount: null,
    highSchoolInstitutionId: null,
    highSchoolLocationId: null,
    highSchoolInstitutionName: null,
    priorHigherEducationStatusId: null,
    motherEducationLevelId: null,
    fatherEducationLevelId: null,
    isMotherOrtGraduate: null,
    isFatherOrtGraduate: null,
    degreeProgramDecisionYearId: null,
    ortDecisionYearId: null,
    researchedOtherUniversities: null,
    decisionSupportId: null,
    decisionLevelId: null,
    hadOrtAdvising: null,
    ortAdvisingRating: null,
    visitedOrtWebsite: null,
    ortWebsiteRating: null,
    visitedOrtCampus: null,
    ortCampusRating: null,
    recallsOrtAdvertising: null,
    ...values,
  };
}

function surveyResponse(
  values: Partial<EnrollmentInitialSurveyResponse> = {}
): EnrollmentInitialSurveyResponse {
  return { ...EMPTY_INITIAL_SURVEY_RESPONSE, ...values };
}

const RESOLVED = {
  fresh: {
    initialSurvey: surveyResponse({ survey: null }),
    loadFailed: false,
  } as EnrollmentInitialSurveyResolved,
  inProgress: (
    section: EnrollmentInitialSurvey['activeSection']
  ): EnrollmentInitialSurveyResolved => ({
    initialSurvey: surveyResponse({ survey: survey({ activeSection: section }) }),
    loadFailed: false,
  }),
  complete: {
    initialSurvey: surveyResponse({ survey: survey({ complete: true }) }),
    loadFailed: false,
  } as EnrollmentInitialSurveyResolved,
  noRight: {
    initialSurvey: surveyResponse({ isEligibleForSurvey: false, survey: null }),
    loadFailed: false,
  } as EnrollmentInitialSurveyResolved,
  loadFailed: { initialSurvey: null, loadFailed: true } as EnrollmentInitialSurveyResolved,
};

const FULL_SUMMARY: EnrollmentSummary = {
  offeringId: 300,
  productId: 20,
  degreeProgram: 'Sistemas',
  intake: 'Marzo 2027',
  shift: 'Noche',
};

const REACTIVATION_RESPONSE: EnrollmentPreEnrollmentResponse = {
  confirmed: false,
  isWaiting: false,
  idEnrollment: 7010,
  paymentDueDate: '2027-03-04',
  enrollmentDeposit: 15500,
  accountBalance: 1200,
  summary: { degreeProgram: 'Sistemas', intake: 'Marzo 2027', shift: 'Noche' },
  seminars: [
    {
      idEnrollment: 7010,
      offeringId: 310,
      name: 'Seminario',
      intake: 'Marzo 2027',
      shift: 'Noche',
    },
  ],
};

function interest(offeringId: number | null): EnrollmentOfferingSummary {
  return { idEnrollment: null, offeringId, name: 'Oferta', intake: null, shift: null };
}

function detail(values: Partial<EnrollmentDetail> = {}): EnrollmentDetail {
  return {
    status: 'En proceso',
    summary: null,
    interests: [],
    pendingPayment: null,
    minimumDeposit: null,
    confirmed: null,
    ...values,
  };
}

// Los params de la URL (idProducto+idProceso) siempre están: son lo que define la
// intención de retomar. El nivel del producto llega resuelto por el resolver; `null` =
// catálogo caído o producto fuera del catálogo.
function resumeEntry(
  detail: EnrollmentDetail | null,
  productLevelId: number | null = null,
  offeringIds: number[] = []
): EnrollmentEntryResolved {
  return {
    intent: 'resume',
    detail,
    productId: 2184,
    admissionProcessId: 122,
    offeringIds,
    productLevelId,
  };
}

function reactivate(
  detail: EnrollmentDetail | null,
  preEnrollment: EnrollmentPreEnrollmentResponse | null = null,
  productLevelId: number | null = null,
  offeringIds: number[] = []
): EnrollmentEntryResolved {
  return {
    intent: 'reactivate',
    detail,
    preEnrollment,
    productId: 2184,
    admissionProcessId: 122,
    offeringIds,
    productLevelId,
  };
}

const DETAIL = {
  inProgressFull: detail({
    status: 'En proceso',
    summary: FULL_SUMMARY,
    interests: [interest(300)],
  }),
  // AP con varios seminarios elegidos: la precarga debe conservar todas las ofertas.
  inProgressMultipleOfferings: detail({
    status: 'En proceso',
    summary: FULL_SUMMARY,
    interests: [interest(310), interest(311), interest(null)],
  }),
  // Sin ofertas de interés no hay con qué reconfirmar: no hay precarga posible.
  inProgressWithoutInterests: detail({
    status: 'En proceso',
    summary: FULL_SUMMARY,
    interests: [],
  }),
  inProgressWithoutSummary: detail({
    status: 'En proceso',
    summary: null,
    interests: [interest(300)],
  }),
  inProgressSummaryWithoutProduct: detail({
    status: 'En proceso',
    summary: { ...FULL_SUMMARY, productId: null },
    interests: [interest(300)],
  }),
  pendingPaymentWithoutDeposit: detail({
    status: 'Pago pendiente',
    pendingPayment: {
      idEnrollment: 1072704,
      deposit: 15500,
      accountBalance: 1200,
      paymentDueDate: '2027-03-04',
      summary: FULL_SUMMARY,
      seminars: [],
    },
  }),
  pendingPaymentWithDeposit: detail({
    status: 'Pago pendiente',
    minimumDeposit: {
      paymentMethod: 'ABITAB',
      documentNumber: '12345678',
      personCode: 555,
      deposit: 3339,
    },
  }),
  pendingWithoutDeposit: detail({
    status: 'Pendiente',
    pendingPayment: {
      idEnrollment: 1072704,
      deposit: 15500,
      accountBalance: 1200,
      paymentDueDate: '2027-03-04',
      summary: FULL_SUMMARY,
      seminars: [],
    },
  }),
  pendingWithDeposit: detail({
    status: 'Pendiente',
    minimumDeposit: {
      paymentMethod: 'ABITAB',
      documentNumber: '12345678',
      personCode: 555,
      deposit: 3339,
    },
  }),
  confirmed: detail({
    status: 'Confirmada',
    confirmed: {
      studentNumber: 397654,
      summary: FULL_SUMMARY,
      academicCoordinator: null,
      courseCoordinator: null,
      enrollments: [],
    },
  }),
  waiting: detail({ status: 'A la espera', summary: null }),
  unknown: detail({ status: null, summary: null }),
  cancelled: detail({ status: 'Cancelada', summary: FULL_SUMMARY }),
};

// Proyección de los campos que forman el contrato de negocio de cada escenario.
function project(state: EnrollmentInitialState) {
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
  entry: EnrollmentEntryResolved,
  survey: EnrollmentInitialSurveyResolved,
  expected: ReturnType<typeof project>,
];

// --- Tabla de escenarios ----------------------------------------------------

describe('deriveInitialEnrollmentState', () => {
  const rows: Row[] = [
    // Intención NUEVA: paso 1 SIEMPRE virgen; la encuesta previa nunca lo prellena.
    [
      'newEnrollment + fresh',
      { intent: 'new' },
      RESOLVED.fresh,
      {
        step: 'proposal',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'newEnrollment + encuesta en-progreso (BUG corregido)',
      { intent: 'new' },
      RESOLVED.inProgress('ort-experience'),
      {
        step: 'proposal',
        survey: 'prefilled',
        activeSection: 'ort-experience',
        includeAcademic: false,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'newEnrollment + encuesta sin seccionActiva',
      { intent: 'new' },
      RESOLVED.inProgress(null),
      {
        step: 'proposal',
        survey: 'prefilled',
        activeSection: 'education',
        includeAcademic: false,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'newEnrollment + encuesta completa',
      { intent: 'new' },
      RESOLVED.complete,
      {
        step: 'proposal',
        survey: 'prefilled',
        activeSection: 'identity',
        includeAcademic: false,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'newEnrollment + sin derecho a encuesta',
      { intent: 'new' },
      RESOLVED.noRight,
      {
        step: 'proposal',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: false,
        payment: 'none',
      },
    ],
    [
      'newEnrollment + encuesta con error de carga',
      { intent: 'new' },
      RESOLVED.loadFailed,
      {
        step: 'proposal',
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
      resumeEntry(null),
      RESOLVED.inProgress('academic-decision'),
      {
        step: 'survey',
        survey: 'prefilled',
        activeSection: 'academic-decision',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar sin detalle (Detalle falló) + fresh',
      resumeEntry(null),
      RESOLVED.fresh,
      {
        step: 'survey',
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
      resumeEntry(DETAIL.inProgressFull),
      RESOLVED.inProgress('ort-experience'),
      {
        step: 'survey',
        survey: 'prefilled',
        activeSection: 'ort-experience',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + fresh',
      resumeEntry(DETAIL.inProgressFull),
      RESOLVED.fresh,
      {
        step: 'survey',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + sin derecho',
      resumeEntry(DETAIL.inProgressFull),
      RESOLVED.noRight,
      {
        step: 'survey',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle sin producto + sin derecho',
      resumeEntry(DETAIL.inProgressSummaryWithoutProduct),
      RESOLVED.noRight,
      {
        step: 'survey',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + completa',
      resumeEntry(DETAIL.inProgressFull),
      RESOLVED.complete,
      {
        step: 'survey',
        survey: 'prefilled',
        activeSection: 'identity',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + detalle full + loadFailed',
      resumeEntry(DETAIL.inProgressFull),
      RESOLVED.loadFailed,
      {
        step: 'survey',
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
      resumeEntry(DETAIL.inProgressSummaryWithoutProduct),
      RESOLVED.inProgress('education'),
      {
        step: 'survey',
        survey: 'prefilled',
        activeSection: 'education',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + sin detalle (bloque null) + en-progreso',
      resumeEntry(DETAIL.inProgressWithoutSummary),
      RESOLVED.inProgress('academic-decision'),
      {
        step: 'survey',
        survey: 'prefilled',
        activeSection: 'academic-decision',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar En proceso + sin ofertas de interés + en-progreso',
      resumeEntry(DETAIL.inProgressWithoutInterests),
      RESOLVED.inProgress('education'),
      {
        step: 'survey',
        survey: 'prefilled',
        activeSection: 'education',
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
      resumeEntry(DETAIL.inProgressMultipleOfferings, 3),
      RESOLVED.fresh,
      {
        step: 'survey',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar AP (nivel 4) En proceso + sin derecho',
      resumeEntry(DETAIL.inProgressFull, 4),
      RESOLVED.noRight,
      {
        step: 'survey',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar AP En proceso + encuesta por-persona en-progreso',
      resumeEntry(DETAIL.inProgressFull, 3),
      RESOLVED.inProgress('education'),
      {
        step: 'survey',
        survey: 'prefilled',
        activeSection: 'education',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar AP En proceso + detalle sin producto + fresh',
      resumeEntry(DETAIL.inProgressSummaryWithoutProduct, 3),
      RESOLVED.fresh,
      {
        step: 'survey',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar nivel 1 En proceso + fresh',
      resumeEntry(DETAIL.inProgressFull, 1),
      RESOLVED.fresh,
      {
        step: 'survey',
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
      resumeEntry(DETAIL.inProgressFull, null),
      RESOLVED.fresh,
      {
        step: 'survey',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
    [
      'retomar AP Pago pendiente sin seña',
      resumeEntry(DETAIL.pendingPaymentWithoutDeposit, 3),
      RESOLVED.fresh,
      {
        step: 'payment',
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
      resumeEntry(DETAIL.pendingPaymentWithoutDeposit),
      RESOLVED.inProgress('education'),
      {
        step: 'payment',
        survey: 'prefilled',
        activeSection: 'education',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'awaiting-method',
      },
    ],
    [
      'retomar Pago pendiente con seña',
      resumeEntry(DETAIL.pendingPaymentWithDeposit),
      RESOLVED.inProgress('education'),
      {
        step: 'payment',
        survey: 'prefilled',
        activeSection: 'education',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'reservation',
      },
    ],
    [
      'retomar Pendiente sin seña',
      resumeEntry(DETAIL.pendingWithoutDeposit),
      RESOLVED.fresh,
      {
        step: 'payment',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'awaiting-method',
      },
    ],
    [
      'retomar Pendiente con seña',
      resumeEntry(DETAIL.pendingWithDeposit),
      RESOLVED.fresh,
      {
        step: 'payment',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'reservation',
      },
    ],
    [
      'retomar Confirmada',
      resumeEntry(DETAIL.confirmed),
      RESOLVED.complete,
      {
        step: 'payment',
        survey: 'prefilled',
        activeSection: 'identity',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'confirmed',
      },
    ],
    [
      'retomar Confirmada + loadFailed (error+retry gana en template)',
      resumeEntry(DETAIL.confirmed),
      RESOLVED.loadFailed,
      {
        step: 'payment',
        survey: 'load-failed',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'confirmed',
      },
    ],
    [
      'retomar A la espera',
      resumeEntry(DETAIL.waiting),
      RESOLVED.inProgress('education'),
      {
        step: 'payment',
        survey: 'prefilled',
        activeSection: 'education',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'in-progress',
      },
    ],
    [
      'retomar estado desconocido/null',
      resumeEntry(DETAIL.unknown),
      RESOLVED.fresh,
      {
        step: 'payment',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'in-progress',
      },
    ],
    [
      'retomar Pago pendiente con seña + sin derecho',
      resumeEntry(DETAIL.pendingPaymentWithDeposit),
      RESOLVED.noRight,
      {
        step: 'payment',
        survey: 'identity-only',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'reservation',
      },
    ],

    // Reactivar usa la respuesta del POST y deja Detalle como fallback.
    [
      'reactivar con seña pendiente',
      reactivate(null, REACTIVATION_RESPONSE),
      RESOLVED.fresh,
      {
        step: 'payment',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'awaiting-method',
      },
    ],
    [
      'reactivar en espera',
      reactivate(null, { ...REACTIVATION_RESPONSE, isWaiting: true }),
      RESOLVED.fresh,
      {
        step: 'payment',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'in-progress',
      },
    ],
    [
      'reactivar con seña cero',
      reactivate(null, { ...REACTIVATION_RESPONSE, enrollmentDeposit: 0 }),
      RESOLVED.fresh,
      {
        step: 'payment',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'reservation',
      },
    ],
    [
      'reactivar sin respuesta usa Detalle como fallback',
      reactivate(DETAIL.cancelled),
      RESOLVED.inProgress('education'),
      {
        step: 'payment',
        survey: 'prefilled',
        activeSection: 'education',
        includeAcademic: false,
        resumeInProgress: true,
        payment: 'in-progress',
      },
    ],
    [
      'reactivar sin respuesta ni Detalle conserva el fallback mínimo',
      reactivate(null),
      RESOLVED.fresh,
      {
        step: 'survey',
        survey: 'fresh',
        activeSection: undefined,
        includeAcademic: undefined,
        resumeInProgress: true,
        payment: 'none',
      },
    ],
  ];

  it.each(rows)('%s', (_name, entry, surveyResolved, expected) => {
    expect(project(deriveInitialEnrollmentState({ entry, survey: surveyResolved }))).toEqual(
      expected
    );
  });

  it('reconstructs preEnrollment (monto/vencimiento/resumen) for pending payment', () => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(DETAIL.pendingPaymentWithoutDeposit),
      survey: RESOLVED.fresh,
    });
    expect(state.preEnrollment).toEqual({
      idEnrollment: 1072704,
      confirmed: false,
      paymentDueDate: '2027-03-04',
      enrollmentDeposit: 15500,
      accountBalance: 1200,
      summary: { degreeProgram: 'Sistemas', intake: 'Marzo 2027', shift: 'Noche' },
      seminars: [],
    });
  });

  it('keeps the Reactivar response as the payment source', () => {
    const state = deriveInitialEnrollmentState({
      entry: reactivate(null, REACTIVATION_RESPONSE),
      survey: RESOLVED.fresh,
    });

    expect(state.preEnrollment).toBe(REACTIVATION_RESPONSE);
  });

  it('maps the chosen deposit method for a reserva outcome', () => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(DETAIL.pendingPaymentWithDeposit),
      survey: RESOLVED.fresh,
    });
    expect(state.payment).toEqual({
      kind: 'reservation',
      method: 'abitab',
      reservation: { documentNumber: '12345678', personCode: 555 },
    });
  });

  it('carries the confirmed detail for a confirmada outcome', () => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(DETAIL.confirmed),
      survey: RESOLVED.complete,
    });
    expect(state.payment).toEqual({ kind: 'confirmed', detail: DETAIL.confirmed.confirmed });
  });

  it('builds the academic prefill from every interest offering when resuming an AP', () => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(DETAIL.inProgressMultipleOfferings, 3),
      survey: RESOLVED.fresh,
    });
    // `turno` y `seminarios` se llenan los dos: el payload de confirmación lee uno u
    // otro según el tipo de propuesta.
    expect(state.academicPrefill).toEqual({
      proposalType: '3',
      degreeProgram: '20',
      intake: '122',
      shift: '310',
      seminars: ['310', '311'],
    });
  });

  it('prefers every offer carried by the dashboard when the detail has no interests', () => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(DETAIL.inProgressWithoutInterests, 4, [310, 311]),
      survey: RESOLVED.fresh,
    });

    expect(state.academicPrefill).toEqual({
      proposalType: '3',
      degreeProgram: '20',
      intake: '122',
      shift: '310',
      seminars: ['310', '311'],
    });
  });

  it('prefers the dashboard offers over stale detail interests', () => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(DETAIL.inProgressFull, 4, [310, 311]),
      survey: RESOLVED.fresh,
    });

    expect(state.academicPrefill).toEqual(
      expect.objectContaining({ shift: '310', seminars: ['310', '311'] })
    );
  });

  it('prefills the academic step for a non-AP resume too', () => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(DETAIL.inProgressFull, 1),
      survey: RESOLVED.inProgress('education'),
    });
    expect(state.academicPrefill).toEqual({
      proposalType: '1',
      degreeProgram: '20',
      intake: '122',
      shift: '300',
      seminars: ['300'],
    });
  });

  it('leaves the proposal type empty when the career catalog failed', () => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(DETAIL.inProgressFull, null),
      survey: RESOLVED.fresh,
    });
    expect(state.academicPrefill).toEqual({
      proposalType: '',
      degreeProgram: '20',
      intake: '122',
      shift: '300',
      seminars: ['300'],
    });
  });

  // Sin Detalle (o sin su bloque de producto) la precarga degrada a los params de la
  // URL: son la prueba de que la inscripción existe.
  it.each([
    ['sin Detalle', null],
    ['con Detalle sin bloque de producto', DETAIL.inProgressWithoutSummary],
  ])('prefills the academic step from the URL params %s', (_name, detail) => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(detail),
      survey: RESOLVED.fresh,
    });
    expect(state.academicPrefill).toEqual({
      proposalType: '',
      degreeProgram: '2184',
      intake: '122',
      shift: detail ? '300' : '',
      seminars: detail ? ['300'] : [],
    });
  });

  it('never prefills nor locks step 1 on a new enrollment', () => {
    const newEnrollment = deriveInitialEnrollmentState({
      entry: { intent: 'new' },
      survey: RESOLVED.fresh,
    });
    expect(newEnrollment.academicPrefill).toBeNull();
    expect(newEnrollment.resumeInProgress).toBe(false);
    expect(newEnrollment.step).toBe('proposal');
  });

  // Invariante duro: con idProducto+idProceso en la URL la inscripción existe, así que
  // ninguna combinación de estado/encuesta puede aterrizar en el paso 1.
  it('never lands on step 1 when resuming, whatever the detail and survey are', () => {
    const details = [null, ...Object.values(DETAIL)];
    const surveys = Object.values(RESOLVED).map(resolved =>
      typeof resolved === 'function' ? resolved('education') : resolved
    );

    for (const detail of details) {
      for (const survey of surveys) {
        for (const productLevelId of [null, 1, 3]) {
          const state = deriveInitialEnrollmentState({
            entry: resumeEntry(detail, productLevelId),
            survey,
          });
          expect(state.step).not.toBe('proposal');
          expect(state.resumeInProgress).toBe(true);
          expect(state.academicPrefill).not.toBeNull();
        }
      }
    }
  });

  it('computes completed sections up to the active one', () => {
    const state = deriveInitialEnrollmentState({
      entry: resumeEntry(DETAIL.inProgressFull),
      survey: RESOLVED.inProgress('ort-experience'),
    });
    expect(state.survey.kind === 'prefilled' && state.survey.completedSections).toEqual([
      'education',
      'academic-decision',
    ]);
  });
});
