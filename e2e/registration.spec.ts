import { expect, test } from '@playwright/test';

import { mockApi } from './support/api-mocks';
import { RegisterPage } from './support/pages/register-page';
import { REGISTER_SCENARIOS } from './support/test-data/register-scenarios';

test.describe('Registration flow guardrails', () => {
  test('completes the new-person registration flow @smoke @regression', async ({ page }) => {
    await mockApi(page, { registerFlow: 'new-person' });

    const register = new RegisterPage(page);
    await register.goto();
    await register.fillIdentity(REGISTER_SCENARIOS['new-person']);
    await register.continueFromIdentity();
    await register.fillFullPersonalData();
    await register.continueFromPersonalData();
    await register.fillCareerSelection();
    await register.submitCareerSelection();
    await register.expectCreatedAccount();
  });

  test('completes the existing-person verification flow @regression', async ({ page }) => {
    await mockApi(page, { registerFlow: 'existing-person' });

    const register = new RegisterPage(page);
    await register.goto();
    await register.fillIdentity(REGISTER_SCENARIOS['existing-person']);
    await register.continueFromIdentity();
    await register.fillVerificationData();
    await register.continueFromPersonalData();
    await register.fillCareerSelection();
    await register.submitCareerSelection();
    await register.expectCreatedAccount();
  });

  test('completes the new-application flow for non-CI documents @regression', async ({ page }) => {
    await mockApi(page, { registerFlow: 'new-application' });

    const register = new RegisterPage(page);
    await register.goto();
    await register.fillIdentity(REGISTER_SCENARIOS['new-application']);
    await register.continueFromIdentity();
    await register.fillFullPersonalData();
    await register.continueFromPersonalData();
    await register.fillCareerSelection();
    await register.submitCareerSelection();
    await register.expectCreatedAccount();
  });

  for (const flow of ['user-exists', 'application-exists'] as const) {
    test(`shows the terminal message and routes back to login for ${flow} @regression`, async ({
      page,
    }) => {
      const scenario = REGISTER_SCENARIOS[flow];
      await mockApi(page, { registerFlow: flow });

      const register = new RegisterPage(page);
      await register.goto();
      await register.fillIdentity(scenario);
      await register.continueFromIdentity();

      await expect(
        page.getByRole('alert').filter({ hasText: scenario.terminalMessage ?? '' })
      ).toBeVisible();
      await page.getByRole('button', { name: 'Iniciar sesión' }).click();
      await expect(page).toHaveURL(/\/iniciar-sesion/);
    });
  }

  test('surfaces required academic selection errors before submit @regression', async ({
    page,
  }) => {
    await mockApi(page, { registerFlow: 'existing-person' });

    const register = new RegisterPage(page);
    await register.goto();
    await register.fillIdentity(REGISTER_SCENARIOS['existing-person']);
    await register.continueFromIdentity();
    await register.fillVerificationData();
    await register.continueFromPersonalData();
    await register.submitCareerSelection();

    await expect(page.getByText('Seleccioná una propuesta académica.')).toBeVisible();
    await expect(page.getByText('Carrera es obligatorio.')).toBeVisible();
    await expect(page.getByText('Comienzo es obligatorio.')).toBeVisible();
  });

  test('surfaces API errors without leaving the identity step @regression', async ({ page }) => {
    await mockApi(page, {
      registerFlow: 'new-person',
      failPaths: ['/Registro/EvaluarDocumento'],
    });

    const register = new RegisterPage(page);
    await register.goto();
    await register.fillIdentity(REGISTER_SCENARIOS['new-person']);
    await register.continueFromIdentity();

    await expect(
      page.getByRole('alert').filter({ hasText: 'No se pudo completar el registro.' }).first()
    ).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Crear cuenta' })).toBeVisible();
  });
});
