import { expect, test } from '@playwright/test';

import { getE2eEnv } from './support/env';
import { LoginPage } from './support/pages/login-page';

test.describe('Preprod controlled nightly flows @nightly @real', () => {
  test.skip(!getE2eEnv('E2E_BASE_URL'), 'Set E2E_BASE_URL to run nightly tests.');

  test('renders the deployed public auth surfaces @nightly @real', async ({ page }) => {
    await page.goto('/iniciar-sesion');
    await expect(page.getByRole('heading', { name: 'Tu inscripción empieza aquí' })).toBeVisible();

    await page.goto('/registro');
    await expect(page.getByRole('heading', { name: 'Crear cuenta' })).toBeVisible();
  });

  test('logs in with seeded preprod credentials @nightly @real', async ({ page }) => {
    const documentNumber = getE2eEnv('E2E_NIGHTLY_DOCUMENT_NUMBER');
    const password = getE2eEnv('E2E_NIGHTLY_PASSWORD');

    if (!documentNumber || !password) {
      test.skip(true, 'Missing seeded preprod E2E credentials.');
      return;
    }

    const login = new LoginPage(page);
    await login.goto();
    await login.login({ documentNumber, password });

    await expect(page).toHaveURL(/\/inicio/);
    await expect(page.getByRole('heading', { name: /Hola/ })).toBeVisible();
  });
});
