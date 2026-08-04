import { expect, type Page, test } from '@playwright/test';

import { expectNoAxeViolations, ORT_FILE_UPLOADER_KNOWN_AXE_ISSUES } from './support/a11y';
import { mockApi } from './support/api-mocks';
import { HomePage } from './support/pages/home-page';
import { InscripcionPage } from './support/pages/inscripcion-page';
import { LoginPage } from './support/pages/login-page';
import { clickRadioByName } from './support/pages/ort-controls';
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
  // El retardo en Pagar permite observar la pantalla "Estamos procesando el pago".
  await mockApi(page, { delayMsByPath: { '/Inscripciones/Pagar': 800 } });
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

  test('traps focus in the enrollment exit dialog and restores it on Escape @a11y', async ({
    page,
  }, testInfo) => {
    await addAuthenticatedSession(page);

    const inscription = new InscripcionPage(page);
    await inscription.goto();

    // En desktop el header oculta el botón de cierre; el disparador visible es el del rail.
    const closeButton =
      testInfo.project.name === 'chromium-mobile'
        ? page.getByRole('button', { name: 'Cerrar inscripción' })
        : page.getByRole('button', { name: 'Salir del proceso' });
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

  test('does not advance enrollment when Enter is pressed on a focused radio @a11y', async ({
    page,
  }) => {
    await addAuthenticatedSession(page);

    const inscription = new InscripcionPage(page);
    await inscription.goto();
    await inscription.expectEnterOnFocusedRadioDoesNotAdvance(
      'tipoPropuesta',
      'Carrera universitaria'
    );
  });

  test('opens the enrollment select drawer with keyboard on mobile @a11y', async ({
    page,
  }, testInfo) => {
    test.skip(testInfo.project.name !== 'chromium-mobile', 'Mobile drawer behavior only.');
    await addAuthenticatedSession(page);

    const inscription = new InscripcionPage(page);
    await inscription.goto();
    await clickRadioByName(page, /^Carrera universitaria/);

    const trigger = page.locator('#academic-proposal-career-mobile');
    await expect(trigger).toBeEnabled();
    await trigger.focus();
    await page.keyboard.press('Enter');

    const dialog = page.getByRole('dialog', { name: 'Seleccionar carrera' });
    await expect(dialog).toBeVisible();
    await expect(trigger).toHaveAttribute('aria-expanded', 'true');
    await expect(trigger).toHaveAttribute('aria-controls', 'academic-proposal-career-drawer');
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
  test('keeps the enrollment survey, payment and confirmation dialog accessible @a11y', async ({
    page,
  }) => {
    await addAuthenticatedSession(page);

    const inscription = new InscripcionPage(page);
    await inscription.goto();
    await inscription.fillAcademicProposal();
    await inscription.fillEducation();
    await inscription.fillAcademicDecision();
    await inscription.fillOrtExperience();

    await expectNoAxeViolations(page, {
      knownIssues: ORT_FILE_UPLOADER_KNOWN_AXE_ISSUES,
    });

    await inscription.fillIdentity();
    await inscription.acceptRegulation();

    await expectNoAxeViolations(page, {
      knownIssues: ORT_FILE_UPLOADER_KNOWN_AXE_ISSUES,
    });

    await inscription.selectPayment('cuenta-personal');

    const dialog = page.getByRole('dialog', { name: 'Confirmar inscripción' });
    await expect(dialog).toBeVisible();
    await expect
      .poll(() => dialog.evaluate(element => element.contains(element.ownerDocument.activeElement)))
      .toBe(true);
    await expectNoAxeViolations(page);

    await page.keyboard.press('Escape');

    await expect(dialog).toBeHidden();
    await expect(inscription.paymentSubmitButton()).toBeVisible();
  });

  test('completes enrollment from start to finish using only the keyboard @a11y @regression', async ({
    page,
  }) => {
    await addAuthenticatedSession(page);

    const inscription = new InscripcionPage(page);
    await inscription.goto();
    await inscription.completeInitialEnrollmentWithKeyboard();

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
