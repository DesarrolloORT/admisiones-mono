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

    // El resumen agrupa los seminarios bajo un unico termino y lista el comienzo
    // y el turno de cada uno; no repite el nombre del seminario.
    await expect(page.getByText('Seminarios', { exact: true })).toBeVisible();
    await expect(page.getByText('Marzo 2027 · Matutino', { exact: true })).toBeVisible();
    await expect(page.getByText('Marzo 2027 · Nocturno', { exact: true })).toBeVisible();

    const paymentRequest = waitForPost(page, '/enrollments/start-payment');
    await enrollment.selectPayment('abitab');

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
    const surveySaveRequests = collectPostRequests(page, '/enrollments/initial-survey');

    await enrollment.goto();
    await enrollment.fillAcademicProposal();
    await enrollment.fillEducation();
    await enrollment.fillAcademicDecision();
    await enrollment.fillOrtExperience();
    await enrollment.fillIdentity();

    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await enrollment.acceptRegulation();

    // La encuesta se guarda por delta a lo largo del paso: la suma de los guardados es la
    // encuesta completa. Al cerrar el paso puede no quedar nada pendiente que postear.
    const savedSurvey: Record<string, unknown> = Object.assign(
      {},
      ...surveySaveRequests.map(request => request.postDataJSON())
    );
    expect(savedSurvey).toMatchObject({
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
        'Tu empresa deberá enviar la solicitud con los datos de la inscripción a sae@ort.edu.uy'
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

    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });

  // Ida y vuelta real: lo respondido en el paso 2 se guarda al salir y aparece precargado al
  // volver a entrar desde el panel principal.
  test('guarda solo lo respondido y lo precarga al volver a entrar @regression', async ({
    page,
  }) => {
    await mockApi(page, { initialSurvey: 'empty', enrollmentDetail: 'in-progress' });
    await addAuthenticatedSession(page);
    const enrollment = new EnrollmentPage(page);
    const surveySaveRequests = collectPostRequests(page, '/enrollments/initial-survey');

    await page.goto('/inicio');
    await continueEnrollment(page);

    await enrollment.answerSurveyRadio('studiesHighSchool', 'Sí, estoy cursando');

    // Una sola respuesta no completa la sección, así que todavía no hay nada posteado: el
    // guardado sale al aparecer el check, al cerrar el paso o al salir, nunca por un timer.
    expect(surveySaveRequests).toHaveLength(0);

    const saveOnExit = waitForPost(page, '/enrollments/initial-survey');
    await enrollment.exitFlow();

    // Solo la respuesta tocada (más la carrera que la encuesta todavía no tenía), nunca la
    // encuesta entera.
    expect((await saveOnExit).postDataJSON()).toEqual({
      degreeProgramId: 20,
      admissionProcessId: 200,
      currentlyInSecondary: true,
    });

    await continueEnrollment(page);

    await expect(enrollment.surveyRadio('studiesHighSchool', 'Sí, estoy cursando')).toBeChecked();
  });

  // Un guardado fallido no pierde el cambio: el snapshot no avanza, así que el delta vuelve
  // completo en el guardado siguiente.
  test('reintenta el delta que falló al completar la sección @regression', async ({ page }) => {
    await mockApi(page, { initialSurvey: 'empty', enrollmentDetail: 'in-progress' });
    await addAuthenticatedSession(page);
    const enrollment = new EnrollmentPage(page);
    let failNextSave = true;
    // Se registra después del mock general: Playwright resuelve el último handler primero y
    // `fallback()` delega en el anterior.
    await page.route('**/enrollments/initial-survey', async route => {
      if (route.request().method() === 'POST' && failNextSave) {
        failNextSave = false;
        return route.fulfill({ status: 500, contentType: 'application/json', body: '{}' });
      }
      return route.fallback();
    });
    const surveySaveRequests = collectPostRequests(page, '/enrollments/initial-survey');

    await page.goto('/inicio');
    await continueEnrollment(page);

    // Completar Educación dispara el guardado, que falla; responder sigue habilitado.
    await enrollment.fillEducation();
    await expect.poll(() => surveySaveRequests.length).toBeGreaterThan(1);

    const failedDelta = surveySaveRequests[0].postDataJSON() as Record<string, unknown>;
    const retried: Record<string, unknown> = Object.assign(
      {},
      ...surveySaveRequests.slice(1).map(request => request.postDataJSON())
    );

    // El delta del POST fallido no se perdió: viaja completo en los guardados siguientes.
    expect(retried).toMatchObject(failedDelta);
  });

  // Los campos de escritura libre actualizan al salir del campo, así que tipear no dispara
  // guardados: sale uno solo al abandonar el campo, cuando la sección queda completa.
  test('no guarda mientras se escribe la institución del exterior @regression', async ({
    page,
  }) => {
    await setup(page, 'empty');
    const enrollment = new EnrollmentPage(page);
    const surveySaveRequests = collectPostRequests(page, '/enrollments/initial-survey');

    await enrollment.goto();
    await enrollment.fillAcademicProposal();
    await enrollment.fillEducationAbroadWithoutInstitution();

    // Falta la institución: la sección no tiene el check todavía.
    expect(surveySaveRequests).toHaveLength(0);

    const institution = enrollment.institutionNameInput();
    await institution.click();
    await institution.pressSequentially('Colegio del exterior');

    // Ni un POST por letra.
    expect(surveySaveRequests).toHaveLength(0);

    const saveOnBlur = waitForPost(page, '/enrollments/initial-survey');
    await institution.blur();

    expect((await saveOnBlur).postDataJSON()).toMatchObject({
      secondaryInstitutionName: 'Colegio del exterior',
    });
    await expect.poll(() => surveySaveRequests.length).toBe(1);
  });

  test('encuesta parcial precargada continúa desde la sección indicada y confirma preinscripción @regression', async ({
    page,
  }) => {
    await setup(page, 'partial');
    const enrollment = new EnrollmentPage(page);
    const surveySaveRequests = collectPostRequests(page, '/enrollments/initial-survey');

    await enrollment.goto('partial');
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

    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await enrollment.acceptRegulation();

    // Guardado por delta: viajan las respuestas nuevas y la carrera que la encuesta parcial
    // todavía no tenía, pero NUNCA lo que vino precargado y no se tocó (los niveles
    // educativos de la madre y el padre ya eran 5 en el backend).
    const savedKeys = surveySaveRequests.flatMap(request =>
      Object.keys((request.postDataJSON() ?? {}) as Record<string, unknown>)
    );
    expect(savedKeys).toContain('degreeProgramId');
    expect(savedKeys).toContain('careerDecisionYearId');
    expect(savedKeys).toContain('hadOrtAdvisory');
    expect(savedKeys).not.toContain('motherEducationLevelId');
    expect(savedKeys).not.toContain('fatherEducationLevelId');
    expect((await preEnrollmentRequest).postDataJSON()).toEqual({
      acceptedRegulations: true,
      isCorporateEnrollment: false,
      selectedOfferingIds: [300],
    });

    await enrollment.selectPayment('personal-account');

    await expect(page.getByRole('heading', { name: '¡Confirmamos tu inscripción!' })).toBeVisible();
  });

  test('encuesta completa oculta la encuesta histórica y genera una reserva @regression', async ({
    page,
  }) => {
    await setup(page, 'complete');
    const enrollment = new EnrollmentPage(page);
    const surveySaveRequests = collectPostRequests(page, '/enrollments/initial-survey');

    await enrollment.goto('survey-complete');

    await expect(page.getByText('Educación', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Decisión académica', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();

    await enrollment.fillIdentity();
    const preEnrollmentRequest = waitForPost(page, '/enrollments/confirm-pre-enrollment');
    await enrollment.acceptRegulation();
    await preEnrollmentRequest;
    // Encuesta ya completada: no se vuelve a postear ni al cerrar el paso.
    expect(surveySaveRequests).toHaveLength(0);
    await enrollment.selectPayment('paganza');

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

    await enrollment.goto('survey-complete');
    await expect(page.getByRole('heading', { name: 'Documento de identidad' })).toBeVisible();
    await enrollment.continueWithPreloadedIdentity();

    expect(documentRequests).toHaveLength(1);
    expect(photoRequests).toHaveLength(1);
  });
  test('muestra el resultado en proceso @regression', async ({ page }) => {
    await setup(page, 'complete');
    const enrollment = new EnrollmentPage(page);

    await enrollment.goto('survey-complete', true);
    await enrollment.fillIdentity();
    await enrollment.acceptRegulation();
    await enrollment.selectPayment('geopay');

    await expect(page.getByRole('heading', { name: 'Inscripción en proceso' })).toBeVisible();
  });

  test('restaura desde backend la sección activa después de recargar @regression', async ({
    page,
  }) => {
    await setup(page, 'partial');
    const enrollment = new EnrollmentPage(page);

    await enrollment.goto('partial');
    await page.reload();

    await expect(
      page.locator('ort-radio-group[formcontrolname="degreeProgramDecisionYear"]')
    ).toBeVisible();
  });
  // Salir guarda lo respondido aunque la seccion siga incompleta, y responder "No" tiene que
  // viajar igual que responder "Si": con `currentlyInSecondary` no anulable, el "No" coincidia
  // con el snapshot inicial y nunca entraba al delta.
  test('guarda las respuestas "No" al salir y las precarga al volver @regression', async ({
    page,
  }) => {
    await mockApi(page, { initialSurvey: 'empty', enrollmentDetail: 'in-progress' });
    await addAuthenticatedSession(page);
    const enrollment = new EnrollmentPage(page);
    const surveySaveRequests = collectPostRequests(page, '/enrollments/initial-survey');

    await page.goto('/inicio');
    await continueEnrollment(page);

    await enrollment.chooseSurveyRadio('studiesHighSchool', 'No');
    await enrollment.chooseSurveyRadio('repeatsHighSchoolYear', 'No');

    const saveOnExit = waitForPost(page, '/enrollments/initial-survey');
    await enrollment.exitFlow();

    expect((await saveOnExit).postDataJSON()).toEqual({
      degreeProgramId: 20,
      admissionProcessId: 200,
      currentlyInSecondary: false,
      repeatsHighSchoolYear: false,
    });
    expect(surveySaveRequests.length).toBeGreaterThan(0);

    await continueEnrollment(page);

    await expect(enrollment.surveyRadio('studiesHighSchool', 'No')).toBeChecked();
    await expect(enrollment.surveyRadio('repeatsHighSchoolYear', 'No')).toBeChecked();
  });

  // La encuesta guarda el id de la institucion pero NO su departamento: al retomar hay que
  // mostrarla igual y dar la seccion por completa, sin obligar a rehacer los dos campos.
  test('precarga la institucion respondida sin departamento al retomar @regression', async ({
    page,
  }) => {
    await mockApi(page, { initialSurvey: 'partial', enrollmentDetail: 'in-progress' });
    await addAuthenticatedSession(page);
    const enrollment = new EnrollmentPage(page);

    await page.goto('/inicio');
    await continueEnrollment(page);

    // Los selects alimentados por catalogo tambien: el valor se escribe antes de que lleguen
    // las opciones, asi que su etiqueta tiene que aparecer al llegar el catalogo.
    await expect(enrollment.educationSelectText('orientation')).toContainText('Científico');
    await expect(enrollment.educationSelectText('motherEducation')).toContainText(
      'Universitaria completa'
    );
    await expect(enrollment.educationSelectText('educationalInstitution')).toContainText(
      'Liceo Nº 1'
    );
    await expect(enrollment.educationSelectText('state')).toContainText('Seleccioná');

    // La seccion es valida sin tocar el departamento: Continuar avanza a Decision academica.
    await page.getByRole('button', { name: 'Continuar', exact: true }).click();

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

async function continueEnrollment(page: Page): Promise<void> {
  await page
    .getByRole('link', { name: /Continuar inscripción/ })
    .first()
    .click();
  await expect(
    page.getByRole('heading', { name: 'Información personal', exact: true })
  ).toBeVisible();
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
