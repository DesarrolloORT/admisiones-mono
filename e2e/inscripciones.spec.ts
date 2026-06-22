import { expect, type Page, type Request, test } from '@playwright/test';

import { mockApi, type MockApiOptions } from './support/api-mocks';
import { InscripcionPage } from './support/pages/inscripcion-page';
import { addAuthenticatedSession } from './support/session';

test.describe('Inscripción inicial', () => {
  test('completa encuesta nueva, confirma preinscripción y confirma el pago @smoke @regression', async ({
    page,
  }) => {
    await setup(page, 'empty');
    const inscription = new InscripcionPage(page);

    await inscription.goto();
    await inscription.fillAcademicProposal();
    await inscription.fillEducation();
    await inscription.fillAcademicDecision();
    await inscription.fillOrtExperience();
    await inscription.fillIdentity();

    const surveyRequest = waitForPost(page, '/Inscripciones/EncuestaInicial');
    const preEnrollmentRequest = waitForPost(page, '/Inscripciones/ConfirmarPreInscripcion');
    await inscription.acceptRegulation();

    expect((await surveyRequest).postDataJSON()).toMatchObject({
      idProducto: 20,
      idProceso: 200,
      ultimoAnioSecundaria: 1,
    });
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      aceptoReglamento: true,
      idOfertaSeleccionada: 300,
    });

    await inscription.selectPayment('cuenta-bancaria');
    await inscription.confirmPayment();

    await expect(page.getByRole('heading', { name: 'Estamos procesando el pago' })).toBeVisible();
    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });

  test('sin derecho a encuesta oculta expansibles de encuesta y no guarda EncuestaInicial @regression', async ({
    page,
  }) => {
    await setup(page, 'no-right');
    const inscription = new InscripcionPage(page);
    const surveySaveRequests = collectPostRequests(page, '/Inscripciones/EncuestaInicial');

    await inscription.goto();
    await inscription.fillAcademicProposal();

    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Decisión académica', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Experiencia con ORT', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();

    await inscription.fillIdentity();
    const preEnrollmentRequest = waitForPost(page, '/Inscripciones/ConfirmarPreInscripcion');
    await inscription.acceptRegulation();

    expect(surveySaveRequests).toHaveLength(0);
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      aceptoReglamento: true,
      idOfertaSeleccionada: 300,
    });

    await inscription.selectPayment('cuenta-bancaria');
    await inscription.confirmPayment();

    await expect(page.getByRole('heading', { name: 'Estamos procesando el pago' })).toBeVisible();
  });

  test('encuesta parcial precargada continúa desde la sección indicada y confirma preinscripción @regression', async ({
    page,
  }) => {
    await setup(page, 'partial');
    const inscription = new InscripcionPage(page);

    await inscription.goto();
    await expect(
      page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
    await expect(
      page.locator('ort-radio-group[formcontrolname="anioDecisionCarrera"]')
    ).toBeVisible();

    await inscription.fillAcademicDecision();
    await inscription.fillOrtExperience();
    await inscription.fillIdentity();

    const surveyRequest = waitForPost(page, '/Inscripciones/EncuestaInicial');
    const preEnrollmentRequest = waitForPost(page, '/Inscripciones/ConfirmarPreInscripcion');
    await inscription.acceptRegulation();

    expect((await surveyRequest).postDataJSON()).toMatchObject({
      idProducto: 20,
      idProceso: 200,
      instruccionMadre: 4,
      instruccionPadre: 4,
    });
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      aceptoReglamento: true,
      idOfertaSeleccionada: 300,
    });

    await inscription.selectPayment('cuenta-personal');
    await inscription.confirmPayment();

    await expect(page.getByRole('heading', { name: 'Estamos procesando el pago' })).toBeVisible();
  });

  test('encuesta completa oculta la encuesta histórica y genera una reserva @regression', async ({
    page,
  }) => {
    await setup(page, 'complete');
    const inscription = new InscripcionPage(page);

    await inscription.goto();

    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Decisión académica', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();

    await inscription.fillIdentity();
    const surveyRequest = waitForPost(page, '/Inscripciones/EncuestaInicial');
    const preEnrollmentRequest = waitForPost(page, '/Inscripciones/ConfirmarPreInscripcion');
    await inscription.acceptRegulation();
    await surveyRequest;
    await preEnrollmentRequest;
    await inscription.selectPayment('paganza');
    await inscription.confirmPayment();

    await expect(page.getByRole('heading', { name: '¡Inscripción reservada!' })).toBeVisible();
    await expect(page.getByText('Buscá Universidad ORT Uruguay')).toBeVisible();
  });

  test('muestra el resultado en proceso @regression', async ({ page }) => {
    await setup(page, 'complete');
    const inscription = new InscripcionPage(page);

    await inscription.goto('encuesta-completa', 'en-proceso');
    await inscription.fillIdentity();
    await inscription.acceptRegulation();
    await inscription.selectPayment('tarjeta-credito');
    await inscription.confirmPayment();

    await expect(page.getByRole('heading', { name: 'Inscripción en proceso' })).toBeVisible();
  });
});

async function setup(page: Page, initialSurvey: MockApiOptions['initialSurvey']): Promise<void> {
  await mockApi(page, { initialSurvey });
  await addAuthenticatedSession(page);
}

function waitForPost(page: Page, path: string): Promise<Request> {
  return page.waitForRequest(request => isPostTo(request, path));
}

function collectPostRequests(page: Page, path: string): Request[] {
  const requests: Request[] = [];
  page.on('request', request => {
    if (isPostTo(request, path)) requests.push(request);
  });
  return requests;
}

function isPostTo(request: Request, path: string): boolean {
  const url = new URL(request.url());
  return request.method() === 'POST' && decodeURIComponent(url.pathname) === path;
}
