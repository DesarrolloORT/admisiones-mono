import { expect, test } from '@playwright/test';

import { mockApi } from './support/api-mocks';
import { getE2eEnv } from './support/env';
import { RegisterPage } from './support/pages/register-page';
import { REGISTER_SCENARIOS } from './support/test-data/register-scenarios';

test.describe('Assisted E2E flows @assisted', () => {
  test.skip(!!process.env['CI'], 'Assisted tests require local human input.');

  test.beforeEach(async ({ page }) => {
    await mockApi(page, { registerFlow: 'new-person' });
  });

  test('prepares registration and lets the tester provide a private document @assisted', async ({
    page,
  }) => {
    const register = new RegisterPage(page);
    const privateDocumentNumber = getE2eEnv('E2E_ASSISTED_DOCUMENT_NUMBER');

    await register.goto();

    const documentNumberField = page.getByRole('textbox', { name: 'Nro. de cédula' });

    if (privateDocumentNumber) {
      await documentNumberField.fill(privateDocumentNumber);
    } else {
      await documentNumberField.focus();
      await page.pause();
    }

    await expect(documentNumberField).not.toHaveValue('');
    await register.continueFromIdentity();
    await register.fillFullPersonalData();
    await register.continueFromPersonalData();
    await register.fillCareerSelection();
    await register.submitCareerSelection();
    await register.expectCreatedAccount();
  });

  test('can still run deterministically with the non-private fixture @assisted', async ({
    page,
  }) => {
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
});
