import { expect, type Page, type Request, test } from '@playwright/test';

import { mockApi, type MockApiOptions } from './support/api-mocks';
import { EnrollmentPage } from './support/pages/enrollment-page';
import { addAuthenticatedSession } from './support/session';

test.describe('Inscripción inicial', () => {
  test('abre el detalle real de un pago pendiente desde Mis carreras @smoke', async ({ page }) => {
    await mockApi(page, { enrollmentDetail: 'pending-payment' });
    await addAuthenticatedSession(page);
    await page.goto('/inicio');

    // Una sola inscripción pendiente: el alert informa la fecha.
    // El detalle del alert está duplicado por breakpoint (span inline / supporting text), así que
    // se busca el que esté visible en el viewport actual.
    await expect(
      page.getByText(/Realizá el pago antes del 15\/03\/2027/).filter({ visible: true })
    ).toBeVisible();

    const detailRequest = page.waitForRequest(request => {
      const url = new URL(request.url());
      return (
        request.method() === 'GET' && decodeURIComponent(url.pathname) === '/enrollments/details'
      );
    });
    await page.getByRole('link', { name: /Ver instrucciones de pago/ }).click();

    const url = new URL((await detailRequest).url());
    expect(url.searchParams.get('productId')).toBe('20');
    expect(url.searchParams.get('admissionProcessId')).toBe('200');
    expect(url.searchParams.get('status')).toBe('Pago pendiente');
    await expect(page.getByRole('heading', { name: '¡Inscripción reservada!' })).toBeVisible();
    await expect(page.getByText('$ 15.500')).toBeVisible();
    await expect(page.getByText(/04\/03\/2027/)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Pagar' })).toHaveCount(0);
  });

  test('dos tarjetas con mismo producto+proceso y distinto estado abren detalles distintos @regression', async ({
    page,
  }) => {
    await mockApi(page, { enrollmentDetail: 'duplicate-status' });
    await addAuthenticatedSession(page);
    await page.goto('/inicio');

    const pendingRequest = page.waitForRequest(request => {
      const url = new URL(request.url());
      return (
        request.method() === 'GET' &&
        decodeURIComponent(url.pathname) === '/enrollments/details' &&
        url.searchParams.get('status') === 'Pago pendiente'
      );
    });
    await page.getByRole('link', { name: /Ver instrucciones de pago/ }).click();
    const pendingUrl = new URL((await pendingRequest).url());
    expect(pendingUrl.searchParams.get('productId')).toBe('20');
    expect(pendingUrl.searchParams.get('admissionProcessId')).toBe('200');
    await expect(page.getByRole('heading', { name: '¡Inscripción reservada!' })).toBeVisible();

    await page.goto('/inicio');
    const confirmedRequest = page.waitForRequest(request => {
      const url = new URL(request.url());
      return (
        request.method() === 'GET' &&
        decodeURIComponent(url.pathname) === '/enrollments/details' &&
        url.searchParams.get('status') === 'Confirmada'
      );
    });
    await page.getByRole('link', { name: /Ver detalle/ }).click();
    const confirmedUrl = new URL((await confirmedRequest).url());
    expect(confirmedUrl.searchParams.get('productId')).toBe('20');
    expect(confirmedUrl.searchParams.get('admissionProcessId')).toBe('200');
  });

  // Actualización profesional nunca postea EncuestaInicial: "Continuar inscripción"
  // conserva las ofertas de la tarjeta aunque el Detalle no las repita.
  test('continúa una actualización profesional en proceso en el paso 2 @regression', async ({
    page,
  }) => {
    await mockApi(page, { enrollmentDetail: 'offers-missing', initialSurvey: 'empty' });
    await addAuthenticatedSession(page);
    await page.goto('/inicio');

    await page
      .getByRole('link', { name: /Continuar inscripción/ })
      .first()
      .click();

    await expect(
      page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
    const resumeUrl = new URL(page.url());
    // `nivel` viaja desde la tarjeta (productLevelId 3 = Actualización profesional) para que
    // el resolver no reconstruya el nivel consultando los tres tipos de propuesta.
    expect(resumeUrl.searchParams.toString()).toBe(
      'idProducto=40&idProceso=210&estado=En+proceso&nivel=3'
    );
    // Sin vuelta atrás al paso 1 y con el flujo reducido de AP.
    await expect(page.getByRole('button', { name: /Volver/ })).toHaveCount(0);
    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);

    // Confirma con TODAS las ofertas de interés, sin volver a registrar el interés.
    const interestRequests = collectPostRequests(page, '/enrollments/product-interest');
    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    const enrollment = new EnrollmentPage(page);
    await enrollment.fillEnrollmentOwnership(false);
    await enrollment.fillIdentity();
    await enrollment.acceptRegulation();

    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      acceptedRegulations: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [310, 311],
    });
    expect(interestRequests).toHaveLength(0);

    await expect(page.getByText('Marco legal y tributario', { exact: true })).toBeVisible();
    await expect(page.getByText('Renta fija y renta variable', { exact: true })).toBeVisible();

    const paymentRequest = waitForPost(page, '/enrollments/start-payment');
    await enrollment.selectPayment('abitab');
    await enrollment.confirmPayment();
    expect((await paymentRequest).postDataJSON()).toEqual({
      enrollmentIds: [7010, 7011],
      paymentType: 'ABITAB',
      sistarbancBankId: null,
    });
  });

  // Con idProducto+idProceso válidos la inscripción existe: aunque el Detalle falle, el
  // paso 1 no puede volver a aparecer.
  test('retoma en el paso 2 aunque el Detalle falle @regression', async ({ page }) => {
    await setup(page, 'empty');

    await page.goto('/inscripciones?idProducto=2184&idProceso=122');

    await expect(
      page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Propuesta académica' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Volver/ })).toHaveCount(0);
  });

  test('completa encuesta nueva, confirma preinscripción y confirma el pago @smoke @regression', async ({
    page,
  }) => {
    await setup(page, 'empty');
    const enrollment = new EnrollmentPage(page);

    await enrollment.goto();
    await enrollment.fillAcademicProposal();
    await enrollment.fillEducation();
    await enrollment.fillAcademicDecision();
    await enrollment.fillOrtExperience();
    await enrollment.fillIdentity();

    const surveyRequest = waitForPost(page, '/enrollments/initial-survey');
    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await enrollment.acceptRegulation();

    expect((await surveyRequest).postDataJSON()).toMatchObject({
      degreeProgramId: 20,
      admissionProcessId: 200,
      lastSecondaryYearLocationId: 1,
    });
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      acceptedRegulations: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    });

    await expect(
      page.getByRole('heading', { name: 'Confirmación', exact: true, level: 2 })
    ).toBeVisible();
    await enrollment.selectPayment('personal-account');
    await enrollment.confirmPayment();
    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });

  test('actualización profesional personal recorre el flujo reducido y abre el pago @regression', async ({
    page,
  }) => {
    await setup(page, 'empty');
    const enrollment = new EnrollmentPage(page);
    const surveySaveRequests = collectPostRequests(page, '/enrollments/initial-survey');

    await enrollment.goto();
    const interestRequest = waitForPost(page, '/enrollments/product-interest');
    await enrollment.fillProfessionalUpdateProposal();

    expect((await interestRequest).postDataJSON()).toEqual({
      admissionProcessId: 210,
      productId: 40,
      offeringIds: [310],
    });

    // Flujo reducido: sin Educación / Decisión académica / Experiencia ORT.
    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Decisión académica', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Experiencia con ORT', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('group', { name: '¿Trabajás actualmente?' })).toHaveCount(0);

    await enrollment.fillEnrollmentOwnership(false);
    await enrollment.fillIdentity();
    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await enrollment.acceptRegulation();

    expect(surveySaveRequests).toHaveLength(0);
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      acceptedRegulations: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [310],
    });

    await expect(
      page.getByRole('heading', { name: 'Confirmación', exact: true, level: 2 })
    ).toBeVisible();
  });

  test('actualización profesional corporativa espera el pago de la empresa @regression', async ({
    page,
  }) => {
    await setup(page, 'empty');
    const enrollment = new EnrollmentPage(page);

    await enrollment.goto();
    await enrollment.fillProfessionalUpdateProposal();
    await enrollment.fillEnrollmentOwnership(true);
    await enrollment.fillIdentity();
    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await enrollment.acceptRegulation('corporate');

    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      acceptedRegulations: true,
      isCorporateEnrollment: true,
      selectedOfferingIds: [310],
    });
    await expect(
      page.getByText(
        'Tu inscripción quedó pendiente del pago de la empresa. Se confirmará automáticamente cuando el pago se acredite.'
      )
    ).toBeVisible();
    await expect(page.getByRole('link', { name: 'Ir al panel principal' })).toHaveAttribute(
      'href',
      '/inicio'
    );
  });

  test('sin derecho a encuesta oculta expansibles de encuesta y no guarda EncuestaInicial @regression', async ({
    page,
  }) => {
    await setup(page, 'no-right');
    const enrollment = new EnrollmentPage(page);
    const surveySaveRequests = collectPostRequests(page, '/enrollments/initial-survey');

    await enrollment.goto();
    await enrollment.fillAcademicProposal();

    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Decisión académica', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Experiencia con ORT', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();

    await enrollment.fillIdentity();
    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await enrollment.acceptRegulation();

    expect(surveySaveRequests).toHaveLength(0);
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      acceptedRegulations: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    });

    await enrollment.selectPayment('personal-account');
    await enrollment.confirmPayment();

    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });

  test('encuesta parcial precargada continúa desde la sección indicada y confirma preinscripción @regression', async ({
    page,
  }) => {
    await setup(page, 'partial');
    const enrollment = new EnrollmentPage(page);

    await enrollment.goto('parcial');
    await expect(
      page.getByRole('heading', { name: 'Información personal', exact: true })
    ).toBeVisible();
    await expect(
      page.locator('ort-radio-group[formcontrolname="studiesHighSchool"]')
    ).toBeVisible();

    await enrollment.fillEducation();
    await enrollment.fillAcademicDecision();
    await enrollment.fillOrtExperience();
    await enrollment.fillIdentity();

    const surveyRequest = waitForPost(page, '/enrollments/initial-survey');
    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await enrollment.acceptRegulation();

    expect((await surveyRequest).postDataJSON()).toMatchObject({
      degreeProgramId: 20,
      admissionProcessId: 200,
      motherEducationLevelId: 5,
      fatherEducationLevelId: 5,
    });
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      acceptedRegulations: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    });

    await enrollment.selectPayment('personal-account');
    await enrollment.confirmPayment();

    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });

  test('encuesta completa oculta la encuesta histórica y genera una reserva @regression', async ({
    page,
  }) => {
    await setup(page, 'complete');
    const enrollment = new EnrollmentPage(page);

    await enrollment.goto('encuesta-completa');

    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Decisión académica', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();

    await enrollment.fillIdentity();
    const surveyRequest = waitForPost(page, '/enrollments/initial-survey');
    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await enrollment.acceptRegulation();
    await surveyRequest;
    await preEnrollmentRequest;
    await enrollment.selectPayment('paganza');
    await enrollment.confirmPayment();

    await expect(page.getByRole('heading', { name: '¡Inscripción reservada!' })).toBeVisible();
    await expect(page.getByText(/Ingresá a Paganza y realizá un nuevo pago/)).toBeVisible();
  });

  test('precarga documento y foto al llegar a verificar identidad @regression', async ({
    page,
  }) => {
    await setup(page, 'complete', 'complete');
    const documentRequests = collectGetRequests(page, '/person/identity-document');
    const photoRequests = collectGetRequests(page, '/person/photo');
    const enrollment = new EnrollmentPage(page);

    await enrollment.goto('encuesta-completa');
    await expect(page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();
    await enrollment.continueWithPreloadedIdentity();

    expect(documentRequests).toHaveLength(1);
    expect(photoRequests).toHaveLength(1);
  });
  test('muestra el resultado en proceso @regression', async ({ page }) => {
    await setup(page, 'complete');
    const enrollment = new EnrollmentPage(page);

    await enrollment.goto('encuesta-completa', 'en-proceso');
    await enrollment.fillIdentity();
    await enrollment.acceptRegulation();
    await enrollment.selectPayment('geopay');
    await enrollment.confirmPayment();

    await expect(page.getByRole('heading', { name: 'Inscripción en proceso' })).toBeVisible();
  });

  test('restaura desde backend la sección activa después de recargar @regression', async ({
    page,
  }) => {
    await setup(page, 'partial');
    const enrollment = new EnrollmentPage(page);

    await enrollment.goto('parcial');
    await page.reload();

    await expect(
      page.locator('ort-radio-group[formcontrolname="degreeProgramDecisionYear"]')
    ).toBeVisible();
  });
  test('retoma la reserva desde el detalle serializable @regression', async ({ page }) => {
    await mockApi(page, { initialSurvey: 'complete', enrollmentDetail: 'pending-payment' });
    await addAuthenticatedSession(page);

    await page.goto(
      '/inscripciones?idProducto=20&idProceso=200&idOferta=300&estado=Pago+pendiente&nivel=1'
    );

    await expect(
      page.getByRole('heading', { name: '¡Inscripción reservada!', level: 1 })
    ).toBeVisible();
    await expect(page.getByText('$ 15.500').first()).toBeVisible();
  });
});

async function setup(
  page: Page,
  initialSurvey: MockApiOptions['initialSurvey'],
  identityPreload: MockApiOptions['identityPreload'] = 'none'
): Promise<void> {
  await mockApi(page, {
    initialSurvey,
    identityPreload,
    ...(initialSurvey === 'partial' || initialSurvey === 'complete'
      ? { enrollmentDetail: 'in-progress' as const }
      : {}),
  });
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

function collectGetRequests(page: Page, path: string): Request[] {
  const requests: Request[] = [];
  page.on('request', request => {
    const url = new URL(request.url());
    if (request.method() === 'GET' && decodeURIComponent(url.pathname) === path) {
      requests.push(request);
    }
  });
  return requests;
}
function isPostTo(request: Request, path: string): boolean {
  const url = new URL(request.url());
  return request.method() === 'POST' && decodeURIComponent(url.pathname) === path;
}
