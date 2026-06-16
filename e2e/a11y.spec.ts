import { expect, test } from '@playwright/test';

import { expectNoAxeViolations } from './support/a11y';
import { mockApi } from './support/api-mocks';
import { HomePage } from './support/pages/home-page';
import { InscripcionPage } from './support/pages/inscripcion-page';
import { LoginPage } from './support/pages/login-page';
import { RegisterPage } from './support/pages/register-page';
import { addAuthenticatedSession } from './support/session';

const publicPages = [
  { path: '/iniciar-sesion', heading: 'Comenzá tu camino en ORT' },
  { path: '/registro', heading: 'Crear cuenta' },
  { path: '/recuperar-acceso', heading: 'Recuperar acceso' },
  { path: '/crear-password?token=a11y-token', heading: 'Creá tu contraseña' },
];

const protectedPages = [
  { path: '/inicio', heading: /Hola/ },
  { path: '/inicio/datos-personales', heading: 'Datos personales' },
  { path: '/inicio/cambiar-contrasena', heading: 'Definí tu nueva contraseña' },
  {
    path: '/inscripciones?escenario=primera-vez',
    heading: 'Inscripción a carrera',
  },
];

test.beforeEach(async ({ page }) => {
  await mockApi(page);
});

test.describe('WCAG axe coverage @a11y', () => {
  for (const pageCase of publicPages) {
    test(`has no axe violations on ${pageCase.path} @a11y`, async ({ page }) => {
      await page.goto(pageCase.path);
      await expect(page.getByRole('heading', { name: pageCase.heading })).toBeVisible();

      await expectNoAxeViolations(page);
    });
  }

  for (const pageCase of protectedPages) {
    test(`has no axe violations on ${pageCase.path} @a11y`, async ({ page }) => {
      await addAuthenticatedSession(page);
      await page.goto(pageCase.path);
      await expect(page.getByRole('heading', { name: pageCase.heading })).toBeVisible();

      await expectNoAxeViolations(page);
    });
  }
});

test.describe('Keyboard and form accessibility @a11y', () => {
  test('keeps invalid login errors discoverable @a11y', async ({ page }) => {
    const login = new LoginPage(page);

    await login.goto();
    await login.submitEmpty();

    await expect(page.getByText('Nro. de documento es obligatorio.')).toBeVisible();
    await expectNoAxeViolations(page);
  });

  test('focuses the register error summary and announces invalid fields @a11y', async ({
    page,
  }) => {
    const register = new RegisterPage(page);

    await register.goto();
    await register.continueFromIdentity();

    const summary = page.locator('ort-error-summary');
    await expect(summary).toBeFocused();
    await expect(summary).toContainText('Nro. de cédula es obligatorio.');

    await expect(page.getByRole('link', { name: 'Nro. de cédula es obligatorio.' })).toHaveCount(0);
  });

  test('closes the profile menu with Escape and restores focus @a11y', async ({ page }) => {
    await addAuthenticatedSession(page);

    const home = new HomePage(page);
    await home.goto();

    const menuButton = home.profileMenuButton();
    await menuButton.focus();
    await page.keyboard.press('Enter');

    await expect(home.profileMenuDialog()).toBeVisible();
    await page.keyboard.press('Escape');

    await expect(home.profileMenuDialog()).toBeHidden();
    await expect(menuButton).toBeFocused();
  });

  test('traps focus in the enrollment exit dialog and restores it on Escape @a11y', async ({
    page,
  }) => {
    await addAuthenticatedSession(page);

    const inscription = new InscripcionPage(page);
    await inscription.goto();

    const closeButton = page.getByRole('button', { name: 'Cerrar inscripción' });
    await closeButton.focus();
    await closeButton.press('Enter');

    const dialog = page.getByRole('dialog', { name: '¿Querés salir de la inscripción?' });
    await expect(dialog).toBeVisible();
    await expect(page.getByRole('button', { name: 'Continuar aquí' })).toBeFocused();
    await expectNoAxeViolations(page);

    await page.keyboard.press('Escape');

    await expect(dialog).toBeHidden();
    await expect(closeButton).toBeFocused();
  });

  test('keeps the enrollment survey, payment and confirmation dialog accessible @a11y', async ({
    page,
  }) => {
    await addAuthenticatedSession(page);

    const inscription = new InscripcionPage(page);
    await inscription.goto('encuesta-completa');
    await inscription.fillAcademicProposal();

    await expectNoAxeViolations(page);

    await inscription.fillIdentity();
    await inscription.acceptRegulation();

    await expectNoAxeViolations(page);

    await inscription.selectPayment('cuenta-bancaria');

    const dialog = page.getByRole('dialog', { name: 'Confirmar inscripción' });
    await expect(dialog).toBeVisible();
    await expect(page.getByRole('button', { name: 'Volver', exact: true })).toBeFocused();
    await expectNoAxeViolations(page);

    await page.keyboard.press('Escape');

    await expect(dialog).toBeHidden();
    await expect(page.getByRole('button', { name: 'Continuar', exact: true })).toBeFocused();
  });
});
