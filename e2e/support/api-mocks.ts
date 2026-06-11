import type { Page, Route } from '@playwright/test';

import type { RegisterFlowKind } from '../../src/app/features/auth/models/register-flow';
import { REGISTER_SCENARIOS, RegisterScenario } from './test-data/register-scenarios';

export interface MockApiOptions {
  registerFlow?: RegisterFlowKind;
  failPaths?: string[];
}

const authenticatedPerson = {
  documento: '12345672',
  primerNombre: 'Ana',
};

export async function mockApi(page: Page, options: MockApiOptions = {}): Promise<void> {
  const registerScenario = REGISTER_SCENARIOS[options.registerFlow ?? 'new-person'];
  const failPaths = new Set(options.failPaths ?? []);

  await page.route('**/*', route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = decodeURIComponent(url.pathname);

    if (!isApiPath(path)) {
      return route.continue();
    }

    if (request.method() === 'OPTIONS') {
      return fulfillCorsPreflight(route);
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

    if (path === '/Persona/DatosPersona' && request.method() === 'GET') {
      return fulfillOperation(route, profileData());
    }

    if (path === '/Persona/DatosPersona' && request.method() === 'PUT') {
      return fulfillOperation(route, true);
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
    path.startsWith('/Persona/') ||
    path.startsWith('/Registro/')
  );
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
