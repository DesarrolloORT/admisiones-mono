import type { Page, Route } from '@playwright/test';

import type { RegisterFlowKind } from '../../src/app/features/auth/models/register-flow';
import { REGISTER_SCENARIOS, RegisterScenario } from './test-data/register-scenarios';

export interface MockApiOptions {
  initialSurvey?: 'empty' | 'partial' | 'complete' | 'no-right';
  identityPreload?: 'none' | 'complete';
  inscriptionDetail?: 'pending-payment';
  registerFlow?: RegisterFlowKind;
  failPaths?: string[];
  delayMsByPath?: Record<string, number>;
}

const authenticatedPerson = {
  documento: '12345672',
  primerNombre: 'Ana',
};

export async function mockApi(page: Page, options: MockApiOptions = {}): Promise<void> {
  const registerScenario = REGISTER_SCENARIOS[options.registerFlow ?? 'new-person'];
  const failPaths = new Set(options.failPaths ?? []);

  await page.route('**/*', async route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = decodeURIComponent(url.pathname);

    if (isRecaptchaScript(url)) {
      return fulfillRecaptchaScript(route);
    }

    if (!isApiPath(path)) {
      return route.continue();
    }

    if (request.method() === 'OPTIONS') {
      return fulfillCorsPreflight(route);
    }

    const delayMs = options.delayMsByPath?.[path] ?? 0;
    if (delayMs > 0) {
      await new Promise(resolve => setTimeout(resolve, delayMs));
    }

    if (failPaths.has(path)) {
      return fulfillApiError(route, 'No se pudo completar el registro.');
    }

    if (path === '/Auth/Login') {
      return fulfillOperation(route, {
        persona: authenticatedPerson,
      });
    }

    if (path === '/Auth/ReenviarCodigo2FA') {
      return fulfillOperation(route, {
        sessionId: 'mock-2fa-session',
        maskedEmail: 'a***@example.com',
        message: 'Código reenviado.',
      });
    }

    if (
      path === '/Auth/ActivarLinkPassword' ||
      path === '/Auth/CompletarPassword' ||
      path === '/Auth/Logout' ||
      path === '/Auth/RecuperarContraseña' ||
      path === '/Persona/CambiarContraseña'
    ) {
      return fulfillOperation(route, true);
    }

    if (path === '/Catalogos/PaisesEstadosCiudades') {
      return fulfillOperation(route, countryLocations());
    }

    if (path === '/Catalogos/Carreras') {
      return fulfillOperation(route, [
        {
          idProducto: 20,
          idNivelProducto: 1,
          nombreProducto: 'Licenciatura en Diseño Gráfico',
          nombreNivelProducto: 'Carrera universitaria',
        },
      ]);
    }

    if (path === '/Catalogos/Comienzos') {
      return fulfillOperation(route, [{ idProceso: 200, nombreProceso: 'Marzo 2027' }]);
    }

    if (path === '/Catalogos/Turnos') {
      return fulfillOperation(route, [
        {
          idOferta: 300,
          horarioReferencia: '08:00 a 12:00',
          turno: { idTurno: 10, nombreTurno: 'Matutino' },
        },
      ]);
    }

    if (path === '/Catalogos/EncuestaInicial') {
      return fulfillOperation(route, initialSurveyCatalogs());
    }

    if (path === '/Persona/Documento' && request.method() === 'GET') {
      return fulfillOperation(
        route,
        options.identityPreload === 'complete'
          ? {
              frente: {
                nombreArchivo: 'documento-frente.png',
                archivo:
                  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQMcAAAAASUVORK5CYII=',
              },
              dorso: {
                nombreArchivo: 'documento-dorso.png',
                archivo:
                  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQMcAAAAASUVORK5CYII=',
              },
              fechaVencimiento: '2030-02-04',
            }
          : {}
      );
    }

    if (path === '/Persona/Foto' && request.method() === 'GET') {
      return route.fulfill({
        contentType: 'image/png',
        headers: corsHeaders(route),
        body:
          options.identityPreload === 'complete'
            ? Buffer.from(
                'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZQMcAAAAASUVORK5CYII=',
                'base64'
              )
            : Buffer.alloc(0),
      });
    }
    if (path === '/Persona/DatosPersona' && request.method() === 'GET') {
      return fulfillOperation(route, profileData());
    }

    if (path === '/Persona/DatosPersona' && request.method() === 'PUT') {
      return fulfillOperation(route, true);
    }

    if (path === '/Persona/Inscripciones' && request.method() === 'GET') {
      return fulfillOperation(
        route,
        options.inscriptionDetail
          ? [
              {
                idProducto: 20,
                idProceso: 200,
                idComienzo: 2,
                idTurno: 3,
                nombreExtensoProducto: 'Licenciatura en Diseño Gráfico',
                nombreComienzo: 'Marzo 2027',
                nombreTurno: 'Matutino',
                estadoInscripcion: 'Pago pendiente',
              },
            ]
          : []
      );
    }

    if (path === '/Persona/Becas' && request.method() === 'GET') {
      return fulfillOperation(route, []);
    }

    if (path === '/Inscripciones/MisInscripciones') {
      return fulfillOperation(route, []);
    }

    if (path === '/Inscripciones/InteresProducto') {
      return fulfillOperation(route, true);
    }

    if (path === '/Inscripciones/Detalle' && request.method() === 'GET') {
      return fulfillOperation(route, {
        estado: 'Pago pendiente',
        pagoPendiente: {
          idInscripcion: 7001,
          senia: 15500,
          fechaVencimientoPago: '2027-03-04',
          resumen: {
            idProducto: 20,
            carrera: 'Licenciatura en Diseño Gráfico',
            idComienzo: 2,
            comienzo: 'Marzo 2027',
            idTurno: 3,
            turno: 'Matutino',
          },
        },
      });
    }

    if (path === '/Inscripciones/EncuestaInicial' && request.method() === 'GET') {
      return fulfillOperation(route, initialSurvey(options.initialSurvey ?? 'empty'));
    }

    if (path === '/Inscripciones/EncuestaInicial' && request.method() === 'POST') {
      return fulfillOperation(route, true);
    }

    if (path === '/Inscripciones/ConfirmarPreInscripcion') {
      return fulfillOperation(route, {
        confirmada: true,
        idInscripcion: 7001,
        seniaInscripcion: 15500,
        fechaVencimientoPago: '2027-03-04',
        resumen: {
          idProducto: 20,
          carrera: 'Licenciatura en Diseño Gráfico',
          idComienzo: 200,
          comienzo: 'Marzo 2027',
          idTurno: 10,
          turno: 'Matutino',
        },
      });
    }

    if (path === '/Registro/EvaluarDocumento') {
      return fulfillRegisterEvaluation(route, registerScenario);
    }

    if (
      path === '/Registro/VerificarIdentidad' ||
      path === '/Registro/ConfirmarNuevaPersona' ||
      path === '/Registro/ConfirmarSolicitudAlta'
    ) {
      if (request.headers()['x-flow-id'] !== registerScenario.flowId) {
        const receivedFlowId = request.headers()['x-flow-id'] ?? '<missing>';

        return fulfillApiError(
          route,
          `Flujo de registro inválido. Esperado ${registerScenario.flowId}, recibido ${receivedFlowId}.`
        );
      }

      return fulfillOperation(route, true);
    }

    if (path === '/Registro/AnalizarAdjunto') {
      return fulfillOperation(route, {});
    }

    return fulfillOperation(route, null);
  });
}

function isApiPath(path: string): boolean {
  return (
    path.startsWith('/Auth/') ||
    path.startsWith('/Catalogos/') ||
    path.startsWith('/Inscripciones/') ||
    path.startsWith('/Persona/') ||
    path.startsWith('/Registro/')
  );
}

function isRecaptchaScript(url: URL): boolean {
  return (
    (url.hostname === 'www.google.com' || url.hostname === 'www.recaptcha.net') &&
    url.pathname === '/recaptcha/api.js'
  );
}

function fulfillRecaptchaScript(route: Route): Promise<void> {
  return route.fulfill({
    contentType: 'application/javascript',
    body: `
      window.grecaptcha = {
        ready: callback => callback(),
        execute: () => Promise.resolve('e2e-captcha-token'),
        render: () => 0,
        reset: () => {},
        getResponse: () => 'e2e-captcha-token'
      };
      window.ng2recaptchaloaded?.();
    `,
  });
}

function fulfillRegisterEvaluation(route: Route, scenario: RegisterScenario): Promise<void> {
  return fulfillOperation(
    route,
    { ...scenario.evaluation, flowId: scenario.flowId },
    {
      message: scenario.terminalMessage ?? null,
    }
  );
}

function fulfillOperation(
  route: Route,
  data: unknown,
  options: { message?: string | null } = {}
): Promise<void> {
  return route.fulfill({
    contentType: 'application/json',
    headers: corsHeaders(route),
    body: JSON.stringify({
      isOperationResult: true,
      success: true,
      httpCode: 200,
      message: options.message ?? null,
      data,
    }),
  });
}

function fulfillApiError(route: Route, message: string): Promise<void> {
  return route.fulfill({
    status: 400,
    contentType: 'application/json',
    headers: corsHeaders(route),
    body: JSON.stringify({
      isOperationResult: true,
      success: false,
      httpCode: 400,
      message,
      data: null,
    }),
  });
}

function fulfillCorsPreflight(route: Route): Promise<void> {
  return route.fulfill({
    status: 204,
    headers: corsHeaders(route),
  });
}

function corsHeaders(route: Route): Record<string, string> {
  return {
    'Access-Control-Allow-Credentials': 'true',
    'Access-Control-Allow-Headers':
      'content-type,x-flow-id,x-captcha-token,x-correlation-id,x-client-session-id',
    'Access-Control-Allow-Methods': 'GET,POST,PUT,PATCH,DELETE,OPTIONS',
    'Access-Control-Allow-Origin': route.request().headers()['origin'] ?? 'http://127.0.0.1:4200',
  };
}

function countryLocations(): unknown[] {
  return [
    {
      codigoPais: 1,
      nombre: 'Uruguay',
      estado: [
        {
          codigoPais: 1,
          codigoEstado: 10,
          nombre: 'Montevideo',
          ciudad: [
            {
              codigoPais: 1,
              codigoEstado: 10,
              codigoCiudad: 100,
              nombre: 'Montevideo',
            },
          ],
        },
      ],
    },
  ];
}

function profileData(): unknown {
  return {
    tipoDocumento: 'CI',
    documento: '12345672',
    primerNombre: 'Gabriela',
    segundoNombre: '',
    primerApellido: 'Ortiz',
    segundoApellido: 'Morales',
    fechaNacimiento: '1988-05-31',
    sexo: 'F',
    codigoPais: 1,
    codigoEstado: 10,
    codigoCiudad: 100,
    direccion: 'Av. 18 de Julio 1360',
    telefono1: '99123456',
    mail: 'gabrielaortiz@example.com',
    verificacionMail: 'gabrielaortiz@example.com',
  };
}

function initialSurveyCatalogs(): unknown {
  return {
    aniosAprobadosEducacionSuperior: [{ value: 1, label: 'Un año' }],
    compartidoCon: [{ value: 1, label: 'Familia' }],
    decisionCarrera: [{ value: 1, label: 'Durante secundaria' }],
    decisionUniversidad: [{ value: 1, label: 'Propuesta académica' }],
    estadoEducacionSuperior: [{ value: 3, label: 'No cursé estudios superiores' }],
    formacionTutores: [{ value: 4, label: 'Universitaria completa' }],
    nivelConocimiento: [{ value: 1, label: 'Conocía bien la propuesta' }],
  };
}

function initialSurvey(kind: NonNullable<MockApiOptions['initialSurvey']>): unknown {
  if (kind === 'no-right') {
    return { tieneDerechoEncuesta: false, encuesta: null };
  }

  if (kind === 'empty') {
    return { tieneDerechoEncuesta: true, encuesta: null };
  }

  const encuesta = {
    idEncuestaIni: 1,
    idProducto: 20,
    idProceso: 200,
    idTurno: 10,
    estadoEncuestaIniAdmision: kind === 'complete' ? 'completa' : 'decision-academica',
    fechaProcesadoEncuestaIni: kind === 'complete' ? '2027-02-01T00:00:00' : null,
    ultimoanioSecundariaEncuestaIni: true,
    codigoInstitucionBac: 1,
    tieneEducacionSuperiorEncuestaIni: 'N',
    instruccionMadreEncuestaIni: '4',
    instruccionPadreEncuestaIni: '4',
    ...(kind === 'complete'
      ? {
          decisionCarreraEncuestaIni: '1',
          decisionUniverEncuestaIni: '2-ems',
          inforOtrasAntesEncuestaIni: 'S',
          nivelDecisionEncuestaIni: true,
          asesoramientoOrtEncuestaIni: 'S',
          vistaSitioWebOrtEncuestaIni: 'S',
          vistaInstalacionesOrtEncuestaIni: 'S',
          publicidadOrtEncuestaIni: 'S',
        }
      : {}),
    producto: { idNivelProducto: 1 },
  };

  return {
    tieneDerechoEncuesta: true,
    encuesta,
    opcionesMotivosSeleccionados:
      kind === 'complete' ? [{ idMotivo: 1, nombreMotivo: 'Propuesta académica' }] : [],
  };
}
