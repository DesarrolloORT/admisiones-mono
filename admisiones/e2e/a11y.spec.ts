import { expect, type Page, test } from '@playwright/test';

import { expectNoAxeViolations, ORT_FILE_UPLOADER_KNOWN_AXE_ISSUES } from './support/a11y';
import { mockApi } from './support/api-mocks';
import { EnrollmentPage } from './support/pages/enrollment-page';
import { HomePage } from './support/pages/home-page';
import { LoginPage } from './support/pages/login-page';
import { clickRadioByName } from './support/pages/ort-controls';
import { RegisterPage } from './support/pages/register-page';
import { addAuthenticatedSession } from './support/session';

const publicPages = [
  { path: '/iniciar-sesion', heading: 'Tu inscripción empieza aquí' },
  { path: '/registro', heading: 'Crear cuenta' },
  { path: '/recuperar-acceso', heading: 'Recuperar contraseña' },
  { path: '/crear-password?token=a11y-token', heading: 'Creá tu contraseña' },
];

const protectedPages = [
  { path: '/inicio', heading: /Hola/ },
  { path: '/inicio/datos-personales', heading: 'Datos personales' },
  { path: '/inicio/cambiar-contrasena', heading: 'Definí tu nueva contraseña' },
  {
    path: '/inscripciones?escenario=primera-vez',
    heading: 'Propuesta académica',
  },
];

test.beforeEach(async ({ page }) => {
  // El retardo en Pagar permite observar la pantalla "Procesando tu pago".
  await mockApi(page, { delayMsByPath: { '/enrollments/start-payment': 800 } });
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
      await expect(getHeading(page, pageCase.heading)).toBeVisible();

      await expectNoAxeViolations(page);
    });
  }
});

test.describe('Keyboard and form accessibility @a11y', () => {
  test.describe.configure({ timeout: 60_000 });
  test('keeps invalid login errors discoverable @a11y', async ({ page }) => {
    const login = new LoginPage(page);

    await login.goto();
    await login.submitEmpty();

    await expect(page.getByText('Revisá los campos marcados.')).toBeVisible();
    await expect(page.getByText('Este campo es obligatorio').first()).toBeVisible();
    await expect(page.locator('#login-document-number')).toBeFocused();
    await expectNoAxeViolations(page);
  });

  test('focuses the register error summary and announces invalid fields @a11y', async ({
    page,
  }) => {
    const register = new RegisterPage(page);

    await register.goto();
    await register.continueFromIdentity();

    const summary = page.locator('ort-error-summary');
    await expect(summary).toContainText('Nro. de cédula es obligatorio.');

    // El resumen recibe foco en un contenedor y luego el foco pasa al primer campo inválido.
    await expect(page.locator('#register-document-number')).toBeFocused();

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

  test('does not advance enrollment when Enter is pressed on a focused radio @a11y', async ({
    page,
  }) => {
    await addAuthenticatedSession(page);

    const enrollment = new EnrollmentPage(page);
    await enrollment.goto();
    await enrollment.expectEnterOnFocusedRadioDoesNotAdvance(
      'proposalType',
      'Carrera universitaria'
    );
  });

  test('opens the enrollment select drawer with keyboard on mobile @a11y', async ({
    page,
  }, testInfo) => {
    test.skip(testInfo.project.name !== 'chromium-mobile', 'Mobile drawer behavior only.');
    await addAuthenticatedSession(page);

    const enrollment = new EnrollmentPage(page);
    await enrollment.goto();
    await clickRadioByName(page, /^Carrera universitaria/);

    const trigger = page.locator('#academic-proposal-degree-program-mobile');
    await expect(trigger).toBeEnabled();
    await trigger.focus();
    await page.keyboard.press('Enter');

    const dialog = page.getByRole('dialog', { name: 'Seleccionar carrera' });
    await expect(dialog).toBeVisible();
    await expect(trigger).toHaveAttribute('aria-expanded', 'true');
    await expect(trigger).toHaveAttribute(
      'aria-controls',
      'academic-proposal-degree-program-drawer'
    );
    await expectNoAxeViolations(page);

    const option = dialog.getByRole('radio', { name: 'Licenciatura en Diseño Gráfico' });
    await option.focus();
    await page.keyboard.press('Space');
    await expect(option).toHaveAttribute('aria-checked', 'true');

    const confirm = page.getByRole('button', { name: 'Seleccionar' });
    await confirm.focus();
    await page.keyboard.press('Enter');

    await expect(dialog).toBeHidden();
    await expect(trigger).toBeFocused();
    await expect(trigger).toContainText('Licenciatura en Diseño Gráfico');
  });
  test('keeps the enrollment survey and payment screens accessible @a11y', async ({ page }) => {
    await addAuthenticatedSession(page);

    const enrollment = new EnrollmentPage(page);
    await enrollment.goto();
    await enrollment.fillAcademicProposal();
    await enrollment.fillEducation();
    await enrollment.fillAcademicDecision();
    await enrollment.fillOrtExperience();

    await expectNoAxeViolations(page, {
      knownIssues: ORT_FILE_UPLOADER_KNOWN_AXE_ISSUES,
    });

    await enrollment.fillIdentity();
    await enrollment.acceptRegulation();

    await expectNoAxeViolations(page, {
      knownIssues: ORT_FILE_UPLOADER_KNOWN_AXE_ISSUES,
    });

    await enrollment.selectPayment('personal-account');

    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
    await expectNoAxeViolations(page);
  });

  test('expands additional subjects only on mobile @a11y', async ({ page }, testInfo) => {
    await mockApi(page, { enrollmentDetail: 'duplicate-status' });
    await addAuthenticatedSession(page);
    await page.goto('/inscripciones?idProducto=20&idProceso=200&estado=Confirmada&nivel=1');

    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
    const subjects = page.locator('#degree-program-subject-list li');
    const subjectsToggle = page.locator('button[aria-controls="degree-program-subject-list"]');

    if (testInfo.project.name === 'chromium-mobile') {
      await expect(subjects).toHaveCount(4);
      await expect(subjectsToggle).toHaveAccessibleName('Ver todas las materias');
      await expect(subjectsToggle).toHaveAttribute('aria-expanded', 'false');
      await subjectsToggle.click();
      await expect(subjects).toHaveCount(5);
      await expect(subjectsToggle).toHaveAccessibleName('Ver menos materias');
      await expect(subjectsToggle).toHaveAttribute('aria-expanded', 'true');
    } else {
      await expect(subjects).toHaveCount(5);
      await expect(subjectsToggle).toHaveCount(0);
    }

    await expectNoAxeViolations(page);
  });

  test('completes enrollment from start to finish using only the keyboard @a11y @regression', async ({
    page,
  }) => {
    await addAuthenticatedSession(page);

    const enrollment = new EnrollmentPage(page);
    await enrollment.goto();
    await enrollment.completeInitialEnrollmentWithKeyboard();

    // El título de procesamiento expone role="status", por lo que no es un heading.
    await expect(page.getByRole('status').filter({ hasText: 'Procesando tu pago' })).toBeVisible();
    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });
});

function getHeading(page: Page, heading: string | RegExp) {
  return page.getByRole('heading', {
    name: heading,
    exact: typeof heading === 'string',
  });
}
