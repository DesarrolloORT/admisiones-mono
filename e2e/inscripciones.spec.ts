import { expect, test } from '@playwright/test';

import { mockApi } from './support/api-mocks';
import { InscripcionPage } from './support/pages/inscripcion-page';
import { addAuthenticatedSession } from './support/session';

const completedSurveyDraftKey = 'inscripcion-borrador:v1:12345672:encuesta-completa';

test.describe('Inscripción inicial', () => {
  test.beforeEach(async ({ page }) => {
    await mockApi(page);
    await addAuthenticatedSession(page);
  });

  test('completa la primera inscripción y confirma el pago @smoke @regression', async ({
    page,
  }) => {
    const inscription = new InscripcionPage(page);

    await inscription.goto();
    await inscription.fillAcademicProposal();
    await inscription.fillEducation();
    await inscription.fillAcademicDecision();
    await inscription.fillOrtExperience();
    await inscription.fillWorkStatus();
    await inscription.fillIdentity();
    await inscription.acceptRegulation();
    await inscription.selectPayment('cuenta-bancaria');
    await inscription.confirmPayment();

    await expect(page.getByRole('heading', { name: 'Estamos procesando el pago' })).toBeVisible();
    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });

  test('oculta la encuesta histórica y genera una reserva @regression', async ({ page }) => {
    const inscription = new InscripcionPage(page);

    await inscription.goto('encuesta-completa');
    await inscription.fillAcademicProposal();

    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Decisión académica', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();

    await inscription.fillIdentity();
    await inscription.acceptRegulation();
    await inscription.selectPayment('paganza');
    await inscription.confirmPayment();

    await expect(page.getByRole('heading', { name: '¡Inscripción reservada!' })).toBeVisible();
    await expect(page.getByText('Buscá Universidad ORT Uruguay')).toBeVisible();
  });

  test('reanuda el escenario parcial desde la primera sección incompleta @regression', async ({
    page,
  }) => {
    const inscription = new InscripcionPage(page);

    await inscription.goto('parcial');
    await expect(
      page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
    await expect(
      page.locator('ort-radio-group[formcontrolname="anioDecisionCarrera"]')
    ).toBeVisible();

    await inscription.fillAcademicDecision();
    await inscription.saveAndExit();
    await inscription.goto('parcial');

    await expect(
      page.getByRole('group', { name: '¿Tuviste una reunión de asesoramiento?' })
    ).toBeVisible();
  });

  test('muestra el resultado en proceso y limpia el borrador terminal @regression', async ({
    page,
  }) => {
    const inscription = new InscripcionPage(page);

    await inscription.goto('encuesta-completa', 'en-proceso');
    await inscription.fillAcademicProposal();
    await inscription.fillIdentity();
    await inscription.acceptRegulation();
    await inscription.selectPayment('tarjeta-credito');
    await inscription.confirmPayment();

    await expect(page.getByRole('heading', { name: 'Inscripción en proceso' })).toBeVisible();
    expect(
      await page.evaluate(key => sessionStorage.getItem(key), completedSurveyDraftKey)
    ).toBeNull();
  });
});
