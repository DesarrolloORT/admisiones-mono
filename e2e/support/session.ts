import type { Page } from '@playwright/test';

const authenticatedSession = {
  token: null,
  documentType: 'CI',
  documentNumber: '12345672',
  primerNombre: 'Ana',
  expiresAt: null,
};

export async function addAuthenticatedSession(page: Page): Promise<void> {
  await page.addInitScript(session => {
    window.localStorage.setItem('auth-session', JSON.stringify(session));
  }, authenticatedSession);
}
