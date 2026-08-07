import { expect, type Page, test } from '@playwright/test';

import { mockApi } from './support/api-mocks';
import { HomePage } from './support/pages/home-page';
import { InscripcionPage } from './support/pages/inscripcion-page';
import { LoginPage } from './support/pages/login-page';
import { RegisterPage } from './support/pages/register-page';
import { personalData, REGISTER_SCENARIOS } from './support/test-data/register-scenarios';

const validPassword = 'Ort2027!Cambio';
const backendDocumentNumber = '1234567-2';

test.describe('Account to enrollment journey', () => {
  test('crea y activa la cuenta, inicia sesión y completa la inscripción @regression', async ({
    page,
  }) => {
    await mockApi(page, { registerFlow: 'new-person' });

    const register = new RegisterPage(page);
    await register.goto();
    await register.fillIdentity(REGISTER_SCENARIOS['new-person']);
    await register.continueFromIdentity();
    await register.fillFullPersonalData();

    const registrationRequest = waitForPost(page, '/registration/confirm-new-person');
    await register.continueFromPersonalData();
    expect((await registrationRequest).postDataJSON()).toMatchObject({
      documentNumber: backendDocumentNumber,
      email: personalData.email,
      firstName: personalData.firstName,
      documentType: REGISTER_SCENARIOS['new-person'].documentType,
    });
    await register.expectCreatedAccount();

    const activationRequest = waitForPost(page, '/auth/activate-password-link');
    await page.goto('/crear-password?token=e2e-account-token');
    expect((await activationRequest).postDataJSON()).toEqual({ token: 'e2e-account-token' });

    await page.getByRole('textbox', { exact: true, name: 'Contraseña' }).fill(validPassword);
    await page.getByRole('textbox', { name: 'Confirmar contraseña' }).fill(validPassword);

    const passwordRequest = waitForPost(page, '/auth/complete-initial-password');
    await page.getByRole('button', { name: 'Activar cuenta' }).click();
    expect((await passwordRequest).postDataJSON()).toEqual({ newPassword: validPassword });
    await expect(page).toHaveURL(/\/inicio/);
    await expect(page.getByRole('heading', { name: /Hola/ })).toBeVisible();

    const home = new HomePage(page);
    await home.profileMenuButton().click();
    await expect(home.profileMenuDialog()).toBeVisible();

    const logoutRequest = waitForPost(page, '/auth/logout');
    await home.profileMenuDialog().getByRole('button', { name: 'Cerrar sesión' }).click();
    await logoutRequest;
    await expect(page).toHaveURL(/\/iniciar-sesion/);

    const login = new LoginPage(page);
    const loginRequest = waitForPost(page, '/auth/login');
    await login.login({
      documentNumber: REGISTER_SCENARIOS['new-person'].documentNumber,
      password: validPassword,
    });
    expect((await loginRequest).postDataJSON()).toMatchObject({
      documentNumber: backendDocumentNumber,
      documentType: REGISTER_SCENARIOS['new-person'].documentType,
    });
    await expect(page).toHaveURL(/\/inicio/);

    await page.getByRole('link', { name: 'Comenzar inscripción' }).click();

    const inscription = new InscripcionPage(page);
    const productInterestRequest = waitForPost(page, '/enrollments/product-interest');
    await inscription.fillAcademicProposal();
    expect((await productInterestRequest).postDataJSON()).toEqual({
      admissionProcessId: 200,
      productId: 20,
      offeringIds: [300],
    });

    await inscription.fillEducation();
    await inscription.fillAcademicDecision();
    await inscription.fillOrtExperience();
    await inscription.fillIdentity();

    const surveyRequest = waitForPost(page, '/enrollments/initial-survey');
    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await inscription.acceptRegulation();
    expect((await surveyRequest).postDataJSON()).toMatchObject({
      degreeProgramId: 20,
      admissionProcessId: 200,
    });
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      acceptedRegulations: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    });

    await inscription.selectPayment('cuenta-personal');
    await inscription.confirmPayment();

    await expect(page.getByRole('heading', { name: 'Estamos procesando el pago' })).toBeVisible();
    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });
});

function waitForPost(page: Page, path: string) {
  return page.waitForRequest(request => {
    const url = new URL(request.url());
    return request.method() === 'POST' && decodeURIComponent(url.pathname) === path;
  });
}
