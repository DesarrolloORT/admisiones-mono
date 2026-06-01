import { expect, Page, TestInfo } from '@playwright/test';

export interface ObservedApiRequest {
  method: string;
  path: string;
  headers: Record<string, string>;
}

const API_PATH_PREFIXES = ['/Auth/', '/Catalogos/', '/Persona/', '/Registro/'];

export function createTelemetryTestRunId(testInfo: TestInfo, suffix = ''): string {
  const title = testInfo.titlePath
    .join('-')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .slice(0, 50);
  const suffixPart = suffix ? `-${suffix}` : '';

  return `pw-${Date.now()}-${testInfo.workerIndex}-${title}${suffixPart}`;
}

export async function addTelemetryTestRun(page: Page, testRunId: string): Promise<void> {
  await page.addInitScript(id => {
    (window as Window & { __TEST_RUN_ID__?: string }).__TEST_RUN_ID__ = id;
  }, testRunId);
}

export function observeAdmisionesApiRequests(page: Page): ObservedApiRequest[] {
  const requests: ObservedApiRequest[] = [];

  page.on('request', request => {
    const path = getAdmisionesApiPath(request.url());

    if (!path) {
      return;
    }

    requests.push({
      method: request.method(),
      path,
      headers: request.headers(),
    });
  });

  return requests;
}

export function getAdmisionesApiPath(url: string): string | null {
  try {
    const path = decodeURIComponent(new URL(url).pathname);

    return API_PATH_PREFIXES.some(prefix => path.startsWith(prefix)) ? path : null;
  } catch {
    return null;
  }
}

export function expectTelemetryHeaders(requests: ObservedApiRequest[], testRunId: string): void {
  expect(requests.length).toBeGreaterThan(0);

  for (const request of requests) {
    expect(request.headers['traceparent']).toMatch(/^00-[a-f0-9]{32}-[a-f0-9]{16}-01$/);
    expect(request.headers['baggage']).toContain('client_route=');
    expect(request.headers['x-correlation-id']).toBeTruthy();
    expect(request.headers['x-client-service']).toBe('admisiones-frontend');
    expect(request.headers['x-client-environment']).toBeTruthy();
    expect(request.headers['x-client-version']).toBeTruthy();
    expect(request.headers['x-client-device']).toBeTruthy();
    expect(request.headers['x-client-route']).toMatch(/^\/[^?#]*$/);
    expect(request.headers['x-test-run-id']).toBe(testRunId);
  }
}
