import { expect, test } from '@playwright/test';

import { mockApi } from './support/api-mocks';
import { addAuthenticatedSession } from './support/session';

test.describe('Scholarships', () => {
  test('lists the catalogue and gates the ones needing an enrollment @smoke', async ({ page }) => {
    await mockApi(page, { scholarshipEnrollments: 'none' });
    await addAuthenticatedSession(page);

    await page.goto('/becas');

    await expect(page.getByRole('heading', { name: 'Postulación a becas' })).toBeVisible();

    // Reválidas no exige inscripción previa: es la única postulable.
    const cards = page.locator('app-scholarship-card');
    await expect(cards).toHaveCount(4);
    await expect(
      page.getByRole('link', { name: /Postularme a Fondo Becas de Reválidas/ })
    ).toHaveAttribute('href', '/becas/fbr');
    await expect(page.getByRole('link', { name: 'Ir a inscripciones' })).toHaveCount(3);
  });

  test('opens every scholarship on the shared process page @regression', async ({ page }) => {
    await mockApi(page, { scholarshipEnrollments: 'confirmed' });
    await addAuthenticatedSession(page);

    // El titulo prueba que `data.kind` de la ruta llega hasta la variante.
    const titles = {
      fbr: 'Fondo de becas de reválidas',
      fexa: 'Fondo de Excelencia Académica',
      fcl: 'Fondo de becas de capacitación laboral',
      fbc: 'Fondo de becas concursables',
    };

    for (const [kind, title] of Object.entries(titles)) {
      await page.goto(`/becas/${kind}`);

      await expect(page.locator('app-scholarship-onboarding-step')).toBeVisible();
      await expect(page.locator('#scholarship-onboarding-title')).toHaveText(title);
    }
  });

  test('walks the fbr application from onboarding to the personal step @regression', async ({
    page,
  }) => {
    await mockApi(page, { scholarshipEnrollments: 'confirmed' });
    await addAuthenticatedSession(page);

    await page.goto('/becas/fbr');

    await page.locator('app-scholarship-onboarding-step button').last().click();

    await expect(page.locator('app-process-layout')).toBeVisible();
    await expect(page.locator('app-scholarship-academic-step')).toBeVisible();
    await expect(page.locator('.sr-only', { hasText: 'Paso 1 de 3' })).toHaveCount(1);

    await page.locator('app-scholarship-academic-step .scholarship-step-button').click();

    await expect(page.locator('app-scholarship-personal-step')).toBeVisible();
    await expect(page.locator('.sr-only', { hasText: 'Paso 2 de 3' })).toHaveCount(1);
  });
});
