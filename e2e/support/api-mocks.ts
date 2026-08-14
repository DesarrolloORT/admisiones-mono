import type { Page, Route } from '@playwright/test';

import type { RegisterFlowKind } from '../../src/app/features/auth/models/register-flow';
import { REGISTER_SCENARIOS, RegisterScenario } from './test-data/register-scenarios';

export interface MockApiOptions {
  initialSurvey?: 'empty' | 'partial' | 'complete' | 'no-right';
  identityPreload?: 'none' | 'complete';
  enrollmentDetail?: 'offers-missing' | 'pending-payment' | 'duplicate-status' | 'in-progress';
  registerFlow?: RegisterFlowKind;
  failPaths?: string[];
  delayMsByPath?: Record<string, number>;
}

const authenticatedPerson = {
  documentNumber: '12345672',
  firstName: 'Ana',
};

export async function mockApi(page: Page, options: MockApiOptions = {}): Promise<void> {
  const registerScenario = REGISTER_SCENARIOS[options.registerFlow ?? 'new-person'];
  const failPaths = new Set(options.failPaths ?? []);

  await page.route('**/*', async route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = decodeURIComponent(url.pathname);

    if (isRecaptchaScript(url)) {
      return fulfillRecaptchaScript(route);
    }

    if (!isApiPath(path)) {
      return route.continue();
    }

    if (request.method() === 'OPTIONS') {
      return fulfillCorsPreflight(route);
    }

    const delayMs = options.delayMsByPath?.[path] ?? 0;
    if (delayMs > 0) {
      await new Promise(resolve => setTimeout(resolve, delayMs));
    }

    if (failPaths.has(path)) {
      return fulfillApiError(route, 'No se pudo completar el registro.');
    }

    if (path === '/auth/login') {
      return fulfillOperation(route, {
        person: authenticatedPerson,
      });
    }

    if (path === '/auth/resend-two-factor-code') {
      return fulfillOperation(route, {
        sessionId: 'mock-2fa-session',
        maskedEmail: 'a***@example.com',
        message: 'Código reenviado.',
      });
    }

    if (
      path === '/auth/activate-password-link' ||
      path === '/auth/complete-initial-password' ||
      path === '/auth/logout' ||
      path === '/auth/recover-password' ||
      path === '/person/change-password' ||
      path === '/person/validate-phone-number'
    ) {
      return fulfillOperation(route, true);
    }

    if (path === '/catalogs/countries-states-cities') {
      return fulfillOperation(route, countryLocations());
    }
    if (path === '/catalogs/degree-programs') {
      if (url.searchParams.get('academicOffer') === '1') {
        return fulfillOperation(route, [
          {
            productLevelId: 1,
            productLevelName: 'Carrera universitaria',
            schools: [
              {
                schoolName: 'Facultad de Diseño',
                products: [{ productId: 20, productName: 'Licenciatura en Diseño Gráfico' }],
              },
            ],
          },
        ]);
      }

      if (url.searchParams.get('academicOffer') === '3') {
        return fulfillOperation(route, [
          {
            productLevelId: 3,
            productLevelName: 'Actualización profesional',
            schools: [
              {
                schoolName: 'Facultad de Administración',
                seminars: [
                  {
                    hasSeminar: true,
                    products: [
                      {
                        productId: 40,
                        admissionProcessId: 210,
                        productName: 'Programa de Asesoramiento Financiero',
                      },
                    ],
                  },
                ],
              },
            ],
          },
        ]);
      }

      return fulfillOperation(route, []);
    }

    if (path === '/catalogs/intakes') {
      // El producto 40 (Actualización profesional) devuelve sus seminars como
      // procesos; el resto conserva el comienzo único del flujo tradicional.
      return fulfillOperation(route, [
        { admissionProcessId: 200, admissionProcessName: 'Marzo 2027' },
      ]);
    }

    if (path === '/catalogs/shifts') {
      if (url.searchParams.get('degreeProgramId') === '40') {
        return fulfillOperation(route, [
          {
            offeringId: 310,
            offeringDescription: 'Marco legal y tributario',
            referenceDate: '2027-03-10',
            referenceSchedule: '',
            shift: { shiftId: 11, shiftName: 'Marco legal y tributario' },
          },
          {
            offeringId: 311,
            offeringDescription: 'Renta fija y renta variable',
            referenceDate: '2027-04-10',
            referenceSchedule: '',
            shift: { shiftId: 12, shiftName: 'Renta fija y renta variable' },
          },
        ]);
      }
      return fulfillOperation(route, [
        {
          offeringId: 300,
          referenceSchedule: '08:00 a 12:00',
          shift: { shiftId: 10, shiftName: 'Matutino' },
        },
      ]);
    }

    if (path === '/catalogs/initial-survey') {
      return fulfillOperation(route, initialSurveyCatalogs());
    }

    if (path === '/catalogs/institutions') {
      return fulfillOperation(route, [{ id: 500, name: 'Liceo Nº 1' }]);
    }

    if (
      (path === '/person/identity-document' || path === '/person/photo') &&
      request.method() === 'POST'
    ) {
      return fulfillOperation(route, true);
    }

    if (path === '/person/identity-document' && request.method() === 'GET') {
      return fulfillOperation(
        route,
        options.identityPreload === 'complete'
          ? {
              front: {
                fileName: 'documento-frente.png',
                content:
                  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQMcAAAAASUVORK5CYII=',
              },
              back: {
                fileName: 'documento-dorso.png',
                content:
                  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQMcAAAAASUVORK5CYII=',
              },
              expirationDate: '2030-02-04',
            }
          : {}
      );
    }

    if (path === '/person/photo' && request.method() === 'GET') {
      return route.fulfill({
        contentType: 'image/png',
        headers: corsHeaders(route),
        body:
          options.identityPreload === 'complete'
            ? Buffer.from(
                'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQMcAAAAASUVORK5CYII=',
                'base64'
              )
            : Buffer.alloc(0),
      });
    }
    if (path === '/person/details' && request.method() === 'GET') {
      return fulfillOperation(route, profileData());
    }

    if (path === '/person/details' && request.method() === 'PUT') {
      return fulfillOperation(route, true);
    }

    if (path === '/person/enrollments' && request.method() === 'GET') {
      // Misma tarjeta producto+proceso, dos tarjetas: sin `status` en el Detalle,
      // ambas resolverían a la misma inscripción.
      if (options.enrollmentDetail === 'duplicate-status') {
        return fulfillOperation(route, [
          {
            productId: 20,
            productFullName: 'Licenciatura en Diseño Gráfico',
            admissionProcessId: 200,
            productLevelId: 1,
            enrollmentStatus: 'Pago pendiente',
            paymentDueDate: '2027-03-15',
            hasSeminars: 'N',
            enrollments: [
              {
                enrollmentId: 7001,
                offeringId: 300,
                offeringDescription: 'Licenciatura en Diseño Gráfico',
                shiftId: 3,
                intakeId: 2,
                intakeStartDate: '2027-03-01',
                intakeName: 'Marzo 2027',
                shiftName: 'Matutino',
                referenceDate: '2027-03-01',
              },
            ],
          },
          {
            productId: 20,
            productFullName: 'Licenciatura en Diseño Gráfico',
            admissionProcessId: 200,
            productLevelId: 1,
            enrollmentStatus: 'Confirmada',
            hasSeminars: 'N',
            enrollments: [
              {
                enrollmentId: 7002,
                offeringId: 300,
                offeringDescription: 'Licenciatura en Diseño Gráfico',
                shiftId: 3,
                intakeId: 2,
                intakeStartDate: '2027-03-01',
                intakeName: 'Marzo 2027',
                shiftName: 'Matutino',
                referenceDate: '2027-03-01',
              },
            ],
          },
        ]);
      }

      return fulfillOperation(
        route,
        options.enrollmentDetail
          ? [
              {
                productId: 20,
                productFullName: 'Licenciatura en Diseño Gráfico',
                admissionProcessId: 200,
                productLevelId: 1,
                enrollmentStatus: 'Pago pendiente',
                paymentDueDate: '2027-03-15',
                hasSeminars: 'N',
                enrollments: [
                  {
                    enrollmentId: 7001,
                    offeringId: 300,
                    offeringDescription: 'Licenciatura en Diseño Gráfico',
                    shiftId: 3,
                    intakeId: 2,
                    intakeStartDate: '2027-03-01',
                    intakeName: 'Marzo 2027',
                    shiftName: 'Matutino',
                    referenceDate: '2027-03-01',
                  },
                ],
              },
              {
                productId: 40,
                productFullName: 'Programa de Asesoramiento Financiero',
                admissionProcessId: 210,
                productLevelId: 3,
                enrollmentStatus: 'En proceso',
                hasSeminars: 'S',
                enrollments: [
                  {
                    enrollmentId: 7010,
                    offeringId: 310,
                    offeringDescription: 'Marco legal y tributario',
                    shiftId: 11,
                    intakeId: 21,
                    intakeStartDate: '2027-03-10',
                    intakeName: 'Marzo 2027',
                    shiftName: 'Matutino',
                    referenceDate: '2027-03-10',
                  },
                  {
                    enrollmentId: 7011,
                    offeringId: 311,
                    offeringDescription: 'Renta fija y renta variable',
                    shiftId: 12,
                    intakeId: 22,
                    intakeStartDate: '2027-04-10',
                    intakeName: 'Marzo 2027',
                    shiftName: 'Nocturno',
                    referenceDate: '2027-04-10',
                  },
                ],
              },
            ]
          : []
      );
    }

    if (path === '/person/scholarships' && request.method() === 'GET') {
      return fulfillOperation(route, []);
    }

    if (path === '/enrollments/product-interest') {
      return fulfillOperation(route, true);
    }

    if (path === '/enrollments/details' && request.method() === 'GET') {
      // Producto que el mock no conoce: el Detalle falla, como pasa con products fuera
      // del catálogo. Retomar tiene que aguantar igual (nunca paso 1).
      if (!['20', '40'].includes(url.searchParams.get('productId') ?? '')) {
        return fulfillApiError(route, 'No se encontró la inscripción.');
      }

      if (options.enrollmentDetail === 'in-progress') {
        return fulfillOperation(route, {
          status: 'En proceso',
          inProgress: {
            summary: { productId: 20, degreeProgram: 'Licenciatura en Diseño Gráfico' },
            interests: [
              {
                offeringId: 300,
                offeringDescription: 'Licenciatura en Diseño Gráfico',
                intake: 'Marzo 2027',
                shift: 'Matutino',
              },
            ],
          },
        });
      }
      // Mismo productId+admissionProcessId, dos tarjetas: solo `status` las distingue.
      if (
        url.searchParams.get('productId') === '20' &&
        url.searchParams.get('admissionProcessId') === '200' &&
        url.searchParams.get('status') === 'Confirmada'
      ) {
        return fulfillOperation(route, {
          status: 'Confirmada',
          confirmed: {
            personId: 7002,
            productId: 20,
            degreeProgram: 'Licenciatura en Diseño Gráfico',
            enrollments: [
              {
                enrollmentId: 7002,
                offeringId: 300,
                intake: 'Marzo 2027',
                shift: 'Matutino',
                firstSemesterSubjects: [],
              },
            ],
          },
        });
      }

      // Actualización profesional en proceso: la cabecera no trae comienzo/turno y
      // las ofertas elegidas llegan en `inProgress.interests` (una por seminario).
      if (url.searchParams.get('productId') === '40') {
        return fulfillOperation(route, {
          status: 'En proceso',
          inProgress: {
            summary: { productId: 40, degreeProgram: 'Programa de Asesoramiento Financiero' },
            interests:
              options.enrollmentDetail === 'offers-missing'
                ? []
                : [
                    {
                      offeringId: 310,
                      offeringDescription: 'Marco legal y tributario',
                      intake: 'Marzo 2027',
                      shift: 'Matutino',
                    },
                    {
                      offeringId: 311,
                      offeringDescription: 'Renta fija y renta variable',
                      intake: 'Marzo 2027',
                      shift: 'Nocturno',
                    },
                  ],
          },
        });
      }

      return fulfillOperation(route, {
        status: 'Pago pendiente',
        pendingPayment: {
          enrollments: [
            {
              enrollmentId: 7001,
              offeringId: 300,
              intake: 'Marzo 2027',
              shift: 'Matutino',
            },
          ],
          depositAmount: 15500,
          summary: {
            productId: 20,
            degreeProgram: 'Licenciatura en Diseño Gráfico',
            paymentDueDate: '2027-03-04',
          },
        },
        minimumDeposit: {
          paymentType: 'ABITAB',
          documentNumber: '12345678',
          personId: 7001,
          depositAmount: 15500,
        },
      });
    }

    if (path === '/enrollments/initial-survey' && request.method() === 'GET') {
      return fulfillOperation(route, initialSurvey(options.initialSurvey ?? 'empty'));
    }

    if (path === '/enrollments/initial-survey' && request.method() === 'POST') {
      return fulfillOperation(route, true);
    }

    if (path === '/enrollments/start-payment') {
      const body = request.postDataJSON() as { paymentType?: string };
      const result = ['ABITAB', 'PAGANZA'].includes(body.paymentType ?? '')
        ? 'Reservada'
        : 'Confirmada';
      return fulfillOperation(route, { result });
    }

    if (path === '/enrollments/confirm-pre-enrollment') {
      const body = request.postDataJSON() as {
        isCorporateEnrollment?: boolean;
        selectedOfferingIds?: number[];
      };
      const selectedOfferingIds = body.selectedOfferingIds ?? [];
      const professionalUpdate = selectedOfferingIds.some(id => id === 310 || id === 311);
      const offerings = selectedOfferingIds.map(offeringId => ({
        enrollmentId: offeringId === 310 ? 7010 : offeringId === 311 ? 7011 : 7001,
        offeringId,
        offeringDescription:
          offeringId === 310
            ? 'Marco legal y tributario'
            : offeringId === 311
              ? 'Renta fija y renta variable'
              : 'Licenciatura en Diseño Gráfico',
        intake: 'Marzo 2027',
        shift: offeringId === 311 ? 'Nocturno' : 'Matutino',
      }));

      return fulfillOperation(route, {
        confirmed: true,
        waiting: body.isCorporateEnrollment === true,
        depositAmount: 15500,
        currentAccount: { currentBalance: 20000 },
        enrollments: offerings,
        summary: {
          productId: professionalUpdate ? 40 : 20,
          degreeProgram: professionalUpdate
            ? 'Programa de Asesoramiento Financiero'
            : 'Licenciatura en Diseño Gráfico',
          paymentDueDate: '2027-03-04',
        },
      });
    }
    if (path === '/registration/evaluate-document') {
      return fulfillRegisterEvaluation(route, registerScenario);
    }

    if (
      path === '/registration/verify-identity' ||
      path === '/registration/confirm-new-person' ||
      path === '/registration/confirm-registration-request'
    ) {
      if (request.headers()['x-flow-id'] !== registerScenario.flowId) {
        const receivedFlowId = request.headers()['x-flow-id'] ?? '<missing>';

        return fulfillApiError(
          route,
          `Flujo de registro inválido. Esperado ${registerScenario.flowId}, recibido ${receivedFlowId}.`
        );
      }

      return fulfillOperation(route, true);
    }

    if (path === '/registration/analyze-attachment') {
      return fulfillOperation(route, {});
    }

    return fulfillOperation(route, null);
  });
}

function isApiPath(path: string): boolean {
  return (
    path.startsWith('/auth/') ||
    path.startsWith('/catalogs/') ||
    path.startsWith('/enrollments/') ||
    path.startsWith('/person/') ||
    path.startsWith('/registration/')
  );
}

function isRecaptchaScript(url: URL): boolean {
  return (
    (url.hostname === 'www.google.com' || url.hostname === 'www.recaptcha.net') &&
    url.pathname === '/recaptcha/api.js'
  );
}

function fulfillRecaptchaScript(route: Route): Promise<void> {
  return route.fulfill({
    contentType: 'application/javascript',
    body: `
      window.grecaptcha = {
        ready: callback => callback(),
        execute: () => Promise.resolve('e2e-captcha-token'),
        render: () => 0,
        reset: () => {},
        getResponse: () => 'e2e-captcha-token'
      };
      window.ng2recaptchaloaded?.();
    `,
  });
}

function fulfillRegisterEvaluation(route: Route, scenario: RegisterScenario): Promise<void> {
  return fulfillOperation(
    route,
    {
      userAlreadyRegistered: scenario.evaluation.userExists,
      requiresIdentityVerification: scenario.evaluation.requiresVerification,
      requiresPersonRegistration: scenario.evaluation.requiresPersonCreation,
      requiresRegistrationRequest: scenario.evaluation.requiresApplicationCreation,
      registrationRequestPending: scenario.evaluation.hasExistingApplication,
      flowId: scenario.flowId,
    },
    {
      message: scenario.terminalMessage ?? null,
    }
  );
}

function fulfillOperation(
  route: Route,
  data: unknown,
  options: { message?: string | null } = {}
): Promise<void> {
  return route.fulfill({
    contentType: 'application/json',
    headers: corsHeaders(route),
    body: JSON.stringify({
      isOperationResult: true,
      success: true,
      httpCode: 200,
      message: options.message ?? null,
      data,
    }),
  });
}

function fulfillApiError(route: Route, message: string): Promise<void> {
  return route.fulfill({
    status: 400,
    contentType: 'application/json',
    headers: corsHeaders(route),
    body: JSON.stringify({
      isOperationResult: true,
      success: false,
      httpCode: 400,
      message,
      data: null,
    }),
  });
}

function fulfillCorsPreflight(route: Route): Promise<void> {
  return route.fulfill({
    status: 204,
    headers: corsHeaders(route),
  });
}

function corsHeaders(route: Route): Record<string, string> {
  return {
    'Access-Control-Allow-Credentials': 'true',
    'Access-Control-Allow-Headers':
      'content-type,x-flow-id,x-captcha-token,x-correlation-id,x-client-session-id',
    'Access-Control-Allow-Methods': 'GET,POST,PUT,PATCH,DELETE,OPTIONS',
    'Access-Control-Allow-Origin': route.request().headers()['origin'] ?? 'http://127.0.0.1:4200',
  };
}

function countryLocations(): unknown[] {
  return [
    {
      countryId: 1,
      name: 'Uruguay',
      states: [
        {
          countryId: 1,
          stateId: 10,
          name: 'Montevideo',
          cities: [
            {
              countryId: 1,
              stateId: 10,
              cityId: 100,
              name: 'Montevideo',
            },
          ],
        },
      ],
    },
  ];
}

function profileData(): unknown {
  return {
    documentType: 'CI',
    documentNumber: '12345672',
    firstName: 'Gabriela',
    middleName: '',
    firstSurname: 'Ortiz',
    secondSurname: 'Morales',
    birthDate: '1988-05-31',
    sex: 'F',
    countryId: 1,
    stateId: 10,
    cityId: 100,
    address: 'Av. 18 de Julio 1360',
    primaryPhone: {
      isValid: true,
      e164: '+59899123456',
      iso2: 'UY',
      countryCode: 598,
      nationalNumber: '99123456',
    },
    email: 'gabrielaortiz@example.com',
    emailConfirmation: 'gabrielaortiz@example.com',
  };
}

function initialSurveyCatalogs(): unknown {
  return {
    education: {
      lastSecondaryYearLocations: [
        { value: 1, label: 'Uruguay' },
        { value: 2, label: 'En el exterior' },
      ],
      highSchoolYears: [
        {
          value: 11,
          label: 'Durante secundaria',
          tracks: [{ value: 12, label: 'Científico', track: 'Matemática' }],
        },
      ],
      previousHigherEducationOptions: [{ value: 3, label: 'No cursé estudios superiores' }],
      universities: [],
      educationLevels: [{ value: 5, label: 'Universitaria completa' }],
    },
    academicDecision: {
      upperSecondaryYears: [
        { value: 1, label: 'Durante secundaria' },
        { value: 2, label: '2º EMS (5º año)' },
      ],
      decisionSupports: [{ value: 1, label: 'Familia' }],
      decisionLevels: [{ value: 1, label: 'Decidido/a' }],
      universities: [],
      ortChoiceReasons: [{ value: 1, label: 'Propuesta académica' }],
    },
    ortExperience: { ratings: [], ortAdvertisements: [] },
  };
}

function initialSurvey(kind: NonNullable<MockApiOptions['initialSurvey']>): unknown {
  if (kind === 'no-right') {
    return { canAnswerSurvey: false, survey: null };
  }

  if (kind === 'empty') {
    return { canAnswerSurvey: true, survey: null };
  }

  const survey = {
    surveyId: 1,
    degreeProgramId: 20,
    admissionProcessId: 200,
    status: kind === 'complete' ? 'completa' : 'educacion',
    currentlyInSecondary: true,
    highSchoolTrackId: 12,
    highSchoolYear: 11,
    repeatsHighSchoolYear: false,
    lastSecondaryYearLocationId: 1,
    secondaryInstitutionId: 500,
    previousHigherEducationId: 3,
    motherEducationLevelId: 5,
    fatherEducationLevelId: 5,
    motherIsOrtGraduate: false,
    fatherIsOrtGraduate: false,
    ...(kind === 'complete'
      ? {
          careerDecisionYearId: 1,
          ortDecisionYearId: 2,
          researchedOtherUniversities: true,
          decisionLevelId: 1,
          hadOrtAdvisory: true,
          visitedOrtWebsite: true,
          visitedOrtFacilities: true,
          recallsOrtAdvertising: true,
          ortChoiceReasonIds: [1],
        }
      : {}),
  };

  return {
    canAnswerSurvey: true,
    survey,
  };
}
