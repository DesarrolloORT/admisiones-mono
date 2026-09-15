import { expect, test } from '@playwright/test';

import { mockApi } from './support/api-mocks';
import { LoginPage } from './support/pages/login-page';
import { addAuthenticatedSession } from './support/session';
import { personalData } from './support/test-data/register-scenarios';

const validPassword = 'Ort2027!Cambio';
const recoverySuccessMessage =
  'Si los datos coinciden, te enviamos un enlace para actualizar tu contraseña.';

test.describe('Base user flows', () => {
  test('logs in and reaches home @smoke @regression', async ({ page }) => {
    await mockApi(page, {
      delayMsByPath: {
        '/auth/login': 500,
        '/person/details': 500,
      },
    });
    const login = new LoginPage(page);

    await login.goto();
    await login.login({ documentNumber: '12345672', password: validPassword });

    const submitButton = page.locator('form button[type="submit"]');
    await expect(submitButton).toBeDisabled();
    await expect(submitButton).toContainText('Validando credenciales');
    await expect(page.locator('app-loader .loader-overlay')).toHaveCount(0);
    await expect(page).toHaveURL(/\/inicio/);
    await expect(page.getByRole('heading', { name: /Hola/ })).toBeVisible();
  });

  test('submits recovery access with controlled data @regression', async ({ page }) => {
    await mockApi(page);
    await page.goto('/recuperar-acceso');
    await expect(page.getByRole('heading', { name: 'Recuperar contraseña' })).toBeVisible();

    await page.getByRole('textbox', { name: 'Nro. de documento' }).fill('12345672');
    await page.getByRole('textbox', { name: 'Primer apellido' }).fill(personalData.firstLastName);
    await page.getByRole('button', { name: 'Enviar' }).click();

    await expect(page).toHaveURL(/\/confirmacion-correo\/recuperar-acceso/);
    await expect(page.getByRole('heading', { name: 'Revisá tu correo' })).toBeVisible();
    await expect(page.getByText(recoverySuccessMessage)).toBeVisible();
  });

  test('creates a password from a valid activation token @regression', async ({ page }) => {
    await mockApi(page, {
      delayMsByPath: {
        '/auth/complete-initial-password': 500,
        '/person/details': 500,
      },
    });
    await page.goto('/crear-password?token=e2e-token');
    await expect(page.getByRole('heading', { name: 'Creá tu contraseña' })).toBeVisible();

    await page.getByRole('textbox', { exact: true, name: 'Contraseña' }).fill(validPassword);
    await page.getByRole('textbox', { name: 'Confirmar contraseña' }).fill(validPassword);
    await page.getByRole('button', { name: 'Activar cuenta' }).click();

    const submitButton = page.locator('form button[type="submit"]');
    await expect(submitButton).toBeDisabled();
    await expect(submitButton).toContainText('Activando...');
    await expect(page.locator('app-loader .loader-overlay')).toHaveCount(0);
    await expect(page).toHaveURL(/\/inicio/);
    await expect(page.getByRole('heading', { name: /Hola/ })).toBeVisible();
  });

  test('updates personal data for an authenticated user @regression', async ({ page }) => {
    await mockApi(page, {
      delayMsByPath: {
        '/catalogs/countries-states-cities': 500,
        '/person/details': 500,
      },
    });
    await addAuthenticatedSession(page);
    await page.goto('/inicio/datos-personales');

    await expect(page.locator('ort-skeleton')).toHaveCount(6);
    await expect(page.locator('app-loader .loader-overlay')).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Datos personales' })).toBeVisible();

    await page.getByRole('textbox', { name: 'Dirección' }).fill('Bulevar España 2633 apto 402');
    await page
      .getByRole('textbox', { exact: true, name: 'E-mail' })
      .fill('gabrielaortiz.updated@example.com');
    await page
      .getByRole('textbox', { name: 'Confirmar e-mail' })
      .fill('gabrielaortiz.updated@example.com');
    await page.getByRole('button', { name: 'Guardar' }).click();

    const submitButton = page.locator('form button[type="submit"]');
    await expect(submitButton).toBeDisabled();
    await expect(submitButton).toContainText('Guardando datos');
    await expect(page.locator('app-loader .loader-overlay')).toHaveCount(0);
    await expect(
      page.getByRole('status').filter({ hasText: 'Datos personales actualizados.' })
    ).toBeVisible();
  });

  test('changes password for an authenticated user @regression', async ({ page }) => {
    await mockApi(page);
    await addAuthenticatedSession(page);
    await page.goto('/inicio/cambiar-contrasena');
    await expect(page.getByRole('heading', { name: 'Definí tu nueva contraseña' })).toBeVisible();

    await page.getByRole('textbox', { name: 'Contraseña actual' }).fill('Anterior2027!');
    await page.getByRole('textbox', { name: 'Nueva contraseña' }).fill(validPassword);
    await page.getByRole('textbox', { name: 'Confirmar contraseña' }).fill(validPassword);
    await page.getByRole('button', { name: 'Guardar nueva contraseña' }).click();

    await expect(page).toHaveURL(/\/inicio/);
  });
});
