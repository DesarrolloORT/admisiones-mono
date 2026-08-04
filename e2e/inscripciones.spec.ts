import { expect, type Page, type Request, test } from '@playwright/test';

import { mockApi, type MockApiOptions } from './support/api-mocks';
import { InscripcionPage } from './support/pages/inscripcion-page';
import { addAuthenticatedSession } from './support/session';

test.describe('Inscripción inicial', () => {
  test('abre el detalle real de un pago pendiente desde Mis carreras @smoke', async ({ page }) => {
    await mockApi(page, { inscriptionDetail: 'pending-payment' });
    await addAuthenticatedSession(page);
    await page.goto('/inicio');

    const detailRequest = page.waitForRequest(request => {
      const url = new URL(request.url());
      return (
        request.method() === 'GET' && decodeURIComponent(url.pathname) === '/Inscripciones/Detalle'
      );
    });
    await page.getByRole('link', { name: /Ver instrucciones de pago/ }).click();

    const url = new URL((await detailRequest).url());
    expect(url.searchParams.get('idProducto')).toBe('20');
    expect(url.searchParams.get('idProceso')).toBe('200');
    await expect(page.getByRole('heading', { name: '¡Inscripción reservada!' })).toBeVisible();
    await expect(page.getByText('$ 15.500')).toBeVisible();
    await expect(page.getByText(/04\/03\/2027/)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Pagar' })).toHaveCount(0);
  });

  // Actualización profesional nunca postea EncuestaInicial: "Continuar inscripción"
  // conserva las ofertas de la tarjeta aunque el Detalle no las repita.
  test('continúa una actualización profesional en proceso en el paso 2 @regression', async ({
    page,
  }) => {
    await mockApi(page, { inscriptionDetail: 'offers-missing', initialSurvey: 'empty' });
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
    expect(resumeUrl.searchParams.toString()).toBe('idProducto=40&idProceso=210');
    // Sin vuelta atrás al paso 1 y con el flujo reducido de AP.
    await expect(page.getByRole('button', { name: /Volver/ })).toHaveCount(0);
    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);

    // Confirma con TODAS las ofertas de interés, sin volver a registrar el interés.
    const interestRequests = collectPostRequests(page, '/Inscripciones/InteresProducto');
    const preEnrollmentRequest = waitForPost(page, '/Inscripciones/ConfirmarPreInscripcion');
    const inscription = new InscripcionPage(page);
    await inscription.fillInscriptionOwnership(false);
    await inscription.fillIdentity();
    await inscription.acceptRegulation();

    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      aceptoReglamento: true,
      esInscripcionCorporativa: false,
      idsOfertasSeleccionadas: [310, 311],
    });
    expect(interestRequests).toHaveLength(0);

    await expect(page.getByText('Marco legal y tributario', { exact: true })).toBeVisible();
    await expect(page.getByText('Renta fija y renta variable', { exact: true })).toBeVisible();

    const paymentRequest = waitForPost(page, '/Inscripciones/Pagar');
    await inscription.selectPayment('abitab');
    await inscription.confirmPayment();
    expect((await paymentRequest).postDataJSON()).toEqual({
      idsInscripcion: [7010, 7011],
      tipoPago: 'ABITAB',
      idBancoSistarbanc: null,
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
      carreraId: 20,
      procesoId: 200,
      ubicacionUltimoAnioSecundariaId: 1,
    });
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      aceptoReglamento: true,
      esInscripcionCorporativa: false,
      idsOfertasSeleccionadas: [300],
    });

    await expect(
      page.getByRole('heading', { name: 'Confirmación', exact: true, level: 2 })
    ).toBeVisible();
    await inscription.selectPayment('cuenta-personal');
    await inscription.confirmPayment();
    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });

  test('actualización profesional personal recorre el flujo reducido y abre el pago @regression', async ({
    page,
  }) => {
    await setup(page, 'empty');
    const inscription = new InscripcionPage(page);
    const surveySaveRequests = collectPostRequests(page, '/Inscripciones/EncuestaInicial');

    await inscription.goto();
    const interestRequest = waitForPost(page, '/Inscripciones/InteresProducto');
    await inscription.fillProfessionalUpdateProposal();

    expect((await interestRequest).postDataJSON()).toEqual({
      idProcesoSeleccionado: 210,
      idProducto: 40,
      idsOferta: [310],
    });

    // Flujo reducido: sin Educación / Decisión académica / Experiencia ORT.
    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Decisión académica', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Experiencia con ORT', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('group', { name: '¿Trabajás actualmente?' })).toHaveCount(0);

    await inscription.fillInscriptionOwnership(false);
    await inscription.fillIdentity();
    const preEnrollmentRequest = waitForPost(page, '/Inscripciones/ConfirmarPreInscripcion');
    await inscription.acceptRegulation();

    expect(surveySaveRequests).toHaveLength(0);
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      aceptoReglamento: true,
      esInscripcionCorporativa: false,
      idsOfertasSeleccionadas: [310],
    });

    await expect(
      page.getByRole('heading', { name: 'Confirmación', exact: true, level: 2 })
    ).toBeVisible();
  });

  test('actualización profesional corporativa espera el pago de la empresa @regression', async ({
    page,
  }) => {
    await setup(page, 'empty');
    const inscription = new InscripcionPage(page);

    await inscription.goto();
    await inscription.fillProfessionalUpdateProposal();
    await inscription.fillInscriptionOwnership(true);
    await inscription.fillIdentity();
    const preEnrollmentRequest = waitForPost(page, '/Inscripciones/ConfirmarPreInscripcion');
    await inscription.acceptRegulation('corporate');

    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      aceptoReglamento: true,
      esInscripcionCorporativa: true,
      idsOfertasSeleccionadas: [310],
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
      esInscripcionCorporativa: false,
      idsOfertasSeleccionadas: [300],
    });

    await inscription.selectPayment('cuenta-personal');
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
      esInscripcionCorporativa: false,
      idsOfertasSeleccionadas: [300],
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

  test('precarga documento y foto al llegar a verificar identidad @regression', async ({
    page,
  }) => {
    await setup(page, 'complete', 'complete');
    const documentRequests = collectGetRequests(page, '/Persona/Documento');
    const photoRequests = collectGetRequests(page, '/Persona/Foto');
    const inscription = new InscripcionPage(page);

    await inscription.goto();
    await expect(page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();
    await inscription.continueWithPreloadedIdentity();

    expect(documentRequests).toHaveLength(1);
    expect(photoRequests).toHaveLength(1);
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

  test('restaura la sección activa después de recargar @regression', async ({ page }) => {
    await setup(page, 'empty');
    const inscription = new InscripcionPage(page);

    await inscription.goto();
    await inscription.fillAcademicProposal();
    await inscription.fillEducation();
    await page.waitForTimeout(350);
    await page.reload();

    await expect(
      page.locator('ort-radio-group[formcontrolname="anioDecisionCarrera"]')
    ).toBeVisible();
  });

  test('restaura el paso de pago con la respuesta serializable @regression', async ({ page }) => {
    await setup(page, 'complete');
    await page.addInitScript(() => {
      window.sessionStorage.setItem(
        'inscripcion-borrador:v1:12345672:encuesta-completa',
        JSON.stringify({
          version: 2,
          escenario: 'encuesta-completa',
          paso: 'pago',
          seccionActiva: 'reglamento',
          seccionesCompletas: ['reglamento'],
          propuesta: {
            tipoPropuesta: '1',
            carrera: '20',
            comienzo: '200',
            turno: '300',
          },
          encuesta: {
            educacion: {},
            decisionAcademica: {},
            experienciaOrt: {},
          },
          identidad: { vencimientoDocumento: '' },
          reglamento: { aceptaReglamento: true },
          pago: { metodoPago: 'cuenta-bancaria' },
          preinscripcion: {
            confirmada: true,
            fechaVencimientoPago: '2027-03-04',
            seniaInscripcion: 15500,
            resumen: {
              carrera: 'Ingeniería en Sistemas',
              comienzo: 'Agosto 2026',
              turno: 'Nocturno',
            },
          },
        })
      );
    });

    await new InscripcionPage(page).goto();

    await expect(
      page.getByRole('heading', { name: 'Confirmación', exact: true, level: 1 })
    ).toBeVisible();
    await expect(page.getByText('$ 15.500').first()).toBeVisible();
  });
});

async function setup(
  page: Page,
  initialSurvey: MockApiOptions['initialSurvey'],
  identityPreload: MockApiOptions['identityPreload'] = 'none'
): Promise<void> {
  await mockApi(page, { initialSurvey, identityPreload });
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
