import { defineConfig, devices } from '@playwright/test';

import { loadE2eEnv } from './e2e/support/env';

loadE2eEnv();

const localBaseURL = 'http://127.0.0.1:4200';
const baseURL = process.env['E2E_BASE_URL'] ?? localBaseURL;
const shouldStartLocalServer = baseURL === localBaseURL;

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: {
    timeout: 5_000,
  },
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL,
    trace: 'retain-on-failure',
  },
  webServer: shouldStartLocalServer
    ? {
        command: 'npm run start -- --host 127.0.0.1 --port 4200',
        reuseExistingServer: !process.env['CI'],
        timeout: 120_000,
        url: baseURL,
      }
    : undefined,
  projects: [
    {
      name: 'chromium-desktop',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1366, height: 900 },
      },
    },
    {
      name: 'chromium-mobile',
      use: devices['Pixel 5'],
    },
  ],
});
