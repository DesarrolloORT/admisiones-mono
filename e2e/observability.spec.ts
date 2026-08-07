import type { Page, TestInfo } from '@playwright/test';
import { expect, test } from '@playwright/test';

import { mockApi } from './support/api-mocks';
import { getE2eBoolean, getE2eEnv, getE2eNumber } from './support/env';
import { RegisterPage } from './support/pages/register-page';
import type { ObservedApiRequest } from './support/telemetry';
import {
  addTelemetryTestRun,
  createTelemetryTestRunId,
  expectTelemetryHeaders,
  getAdmisionesApiPath,
  observeAdmisionesApiRequests,
} from './support/telemetry';
import {
  personalData,
  REGISTER_SCENARIOS,
  RegisterScenario,
} from './support/test-data/register-scenarios';

const registrationFlows = ['new-person', 'existing-person', 'new-application'] as const;
const telemetryBatchSize = Math.max(
  1,
  Math.min(getE2eNumber('E2E_TELEMETRY_REGISTRATION_BATCH_SIZE', 3), 50)
);
const backendObservabilityEnabled = getE2eBoolean('E2E_BACKEND_OBSERVABILITY');
const rateLimitProbeEnabled = getE2eBoolean('E2E_RATE_LIMIT_PROBE');

test.describe('Observable registration telemetry', () => {
  for (let index = 0; index < telemetryBatchSize; index += 1) {
    const flow = registrationFlows[index % registrationFlows.length];

    test(`registration flow emits telemetry headers ${index + 1}/${telemetryBatchSize} (${flow}) @telemetry`, async ({
      page,
    }, testInfo) => {
      const testRunId = await prepareTelemetry(page, testInfo, `mock-${flow}-${index + 1}`);
      const requests = observeAdmisionesApiRequests(page);

      await mockApi(page, { registerFlow: flow });
      await completeMockedRegistration(page, REGISTER_SCENARIOS[flow], testRunId);

      const observed = relevantRegistrationRequests(requests);

      expectTelemetryHeaders(observed, testRunId);
      expect(observed.map(request => request.path)).toEqual(
        expect.arrayContaining(['/registration/evaluate-document'])
      );

      console.log(`[telemetry] test_run_id=${testRunId}`);
    });
  }
});

test.describe('Backend observability probes', () => {
  test('runs a full registration flow with backend telemetry @backend-telemetry', async ({
    page,
  }, testInfo) => {
    test.skip(
      !backendObservabilityEnabled,
      'Set E2E_BACKEND_OBSERVABILITY=true to run this probe against a real backend.'
    );
    test.skip(testInfo.project.name !== 'chromium-desktop', 'Backend probes run once on desktop.');

    const testRunId = await prepareTelemetry(page, testInfo, 'real-registration');
    const requests = observeAdmisionesApiRequests(page);
    const register = new RegisterPage(page);

    await register.goto();
    await register.fillIdentity(realBackendScenario(testRunId));
    await register.continueFromIdentity();
    await register.fillFullPersonalData(realBackendPersonalData(testRunId));
    await register.continueFromPersonalData();
    await register.expectCreatedAccount();

    const observed = relevantRegistrationRequests(requests);

    expectTelemetryHeaders(observed, testRunId);
    expect(observed.map(request => request.path)).toEqual(
      expect.arrayContaining(['/registration/evaluate-document'])
    );

    console.log(`[backend-telemetry] test_run_id=${testRunId}`);
  });

  test('probes rate limiting on Registro/AnalizarAdjunto @backend-rate-limit', async ({
    page,
  }, testInfo) => {
    test.skip(
      !rateLimitProbeEnabled,
      'Set E2E_RATE_LIMIT_PROBE=true to actively hit the rate-limited endpoint.'
    );
    test.skip(
      testInfo.project.name !== 'chromium-desktop',
      'Rate limit probe runs once on desktop.'
    );

    const attempts = Math.max(1, getE2eNumber('E2E_RATE_LIMIT_ATTEMPTS', 20));
    test.setTimeout(Math.max(60_000, attempts * 20_000));

    const testRunId = await prepareTelemetry(page, testInfo, 'rate-limit');
    const requests = observeAdmisionesApiRequests(page);
    const statuses: number[] = [];
    const register = new RegisterPage(page);

    await register.goto();

    for (let index = 0; index < attempts; index += 1) {
      const responsePromise = page.waitForResponse(
        response => getAdmisionesApiPath(response.url()) === '/registration/analyze-attachment',
        { timeout: 60_000 }
      );

      await register.scanDocument(documentProbeFile(index));

      const response = await responsePromise;
      statuses.push(response.status());

      if (response.status() === 429) {
        break;
      }

      await page
        .getByText('Procesando documento...')
        .waitFor({ state: 'hidden', timeout: 10_000 })
        .catch(() => undefined);
    }

    const observed = requests.filter(
      request => request.path === '/registration/analyze-attachment'
    );

    expectTelemetryHeaders(observed, testRunId);
    expect(statuses).toContain(429);
    console.log(`[backend-rate-limit] test_run_id=${testRunId} statuses=${statuses.join(',')}`);
  });
});

async function prepareTelemetry(page: Page, testInfo: TestInfo, suffix: string): Promise<string> {
  const testRunId = createTelemetryTestRunId(testInfo, suffix);

  await addTelemetryTestRun(page, testRunId);

  return testRunId;
}

async function completeMockedRegistration(
  page: Page,
  scenario: RegisterScenario,
  testRunId: string
): Promise<void> {
  const register = new RegisterPage(page);
  const personal = {
    ...personalData,
    email: telemetryEmail(testRunId),
  };

  await register.goto();
  await register.fillIdentity(scenario);
  await register.continueFromIdentity();

  if (scenario.kind === 'existing-person') {
    await register.fillVerificationData(personal);
    await register.continueFromPersonalData();
    await register.expectVerifiedIdentity();
    return;
  } else {
    await register.fillFullPersonalData(personal);
  }

  await register.continueFromPersonalData();
  await register.expectCreatedAccount();
}

function relevantRegistrationRequests(requests: ObservedApiRequest[]): ObservedApiRequest[] {
  return requests.filter(
    request => request.path.startsWith('/registration/') || request.path.startsWith('/catalogs/')
  );
}

function realBackendScenario(testRunId: string): RegisterScenario {
  const documentType = realDocumentType();

  return {
    kind: 'new-application',
    documentType,
    documentNumber:
      getE2eEnv('E2E_REAL_REGISTER_DOCUMENT_NUMBER') ?? `PW-${testRunId.slice(0, 24)}`,
    flowId: '',
    evaluation: {
      requiereAltaPersona: false,
      requiereAltaSolicitud: true,
      requiereVerificacion: false,
      solicitudAltaExistente: false,
      usuarioExistente: false,
    },
  };
}

function realDocumentType(): RegisterScenario['documentType'] {
  const value = getE2eEnv('E2E_REAL_REGISTER_DOCUMENT_TYPE');

  return value === 'CI' || value === 'PS' || value === 'DE' ? value : 'PS';
}

function realBackendPersonalData(): typeof personalData;
function realBackendPersonalData(testRunId: string): typeof personalData;
function realBackendPersonalData(testRunId = String(Date.now())): typeof personalData {
  return {
    ...personalData,
    email: getE2eEnv('E2E_REAL_REGISTER_EMAIL') ?? telemetryEmail(testRunId),
  };
}

function telemetryEmail(seed: string): string {
  return `telemetry.${seed.replace(/[^a-z0-9]+/gi, '').slice(0, 32)}@example.com`;
}

function documentProbeFile(index: number): { name: string; mimeType: string; buffer: Buffer } {
  return {
    name: `rate-limit-probe-${Date.now()}-${index}.png`,
    mimeType: 'image/png',
    buffer: Buffer.from(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=',
      'base64'
    ),
  };
}
