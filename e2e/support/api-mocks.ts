import type { Page, Route } from '@playwright/test';

import type { RegisterFlowKind } from '../../src/app/features/auth/models/register-flow';
import { REGISTER_SCENARIOS, RegisterScenario } from './test-data/register-scenarios';

export interface MockApiOptions {
  initialSurvey?: 'empty' | 'partial' | 'complete' | 'no-right';
  identityPreload?: 'none' | 'complete';
  inscriptionDetail?: 'offers-missing' | 'pending-payment';
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
      if (url.searchParams.get('propuestaAcademica') === '1') {
        return fulfillOperation(route, [
          {
            idNivelProducto: 1,
            nombreNivelProducto: 'Carrera universitaria',
            escuelas: [
              {
                nombreEscuela: 'Facultad de Diseño',
                productos: [{ idProducto: 20, nombreProducto: 'Licenciatura en Diseño Gráfico' }],
              },
            ],
          },
        ]);
      }

      if (url.searchParams.get('propuestaAcademica') === '3') {
        return fulfillOperation(route, [
          {
            idNivelProducto: 3,
            nombreNivelProducto: 'Actualización profesional',
            escuelas: [
              {
                nombreEscuela: 'Facultad de Administración',
                seminarios: [
                  {
                    tieneSeminario: true,
                    productos: [
                      {
                        idProducto: 40,
                        idProceso: 210,
                        nombreProducto: 'Programa de Asesoramiento Financiero',
                      },
                    ],
                  },
                ],
              },
            ],
          },
        ]);
      }

      return fulfillOperation(route, []);
    }

    if (path === '/Catalogos/Comienzos') {
      // El producto 40 (Actualización profesional) devuelve sus seminarios como
      // procesos; el resto conserva el comienzo único del flujo tradicional.
      return fulfillOperation(route, [{ idProceso: 200, nombreProceso: 'Marzo 2027' }]);
    }

    if (path === '/Catalogos/Turnos') {
      if (url.searchParams.get('idCarrera') === '40') {
        return fulfillOperation(route, [
          {
            idOferta: 310,
            descripcionOferta: 'Marco legal y tributario',
            fechaReferencia: '2027-03-10',
            horarioReferencia: '',
            turno: { idTurno: 11, nombreTurno: 'Marco legal y tributario' },
          },
          {
            idOferta: 311,
            descripcionOferta: 'Renta fija y renta variable',
            fechaReferencia: '2027-04-10',
            horarioReferencia: '',
            turno: { idTurno: 12, nombreTurno: 'Renta fija y renta variable' },
          },
        ]);
      }
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

    if (path === '/Catalogos/Instituciones') {
      return fulfillOperation(route, [
        { codigoEmpresa: 500, nombre: 'Liceo Nº 1', codigoPais: 1, codigoEstado: 10 },
      ]);
    }

    if (path === '/Persona/SubirDocumento' || path === '/Persona/SubirFoto') {
      return fulfillOperation(route, true);
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
                nombreExtensoProducto: 'Licenciatura en Diseño Gráfico',
                idProceso: 200,
                idNivelProducto: 1,
                estadoInscripcion: 'Pago pendiente',
                fechaVencimientoPago: '2027-03-15',
                progConSeminariosProducto: 'N',
                inscripciones: [
                  {
                    idInscripto: 7001,
                    idOferta: 300,
                    descripcionOferta: 'Licenciatura en Diseño Gráfico',
                    idTurno: 3,
                    idComienzo: 2,
                    fechaInicioComienzo: '2027-03-01',
                    nombreComienzo: 'Marzo 2027',
                    nombreTurno: 'Matutino',
                    fechaReferencia: '2027-03-01',
                  },
                ],
              },
              {
                idProducto: 40,
                nombreExtensoProducto: 'Programa de Asesoramiento Financiero',
                idProceso: 210,
                idNivelProducto: 3,
                estadoInscripcion: 'En proceso',
                progConSeminariosProducto: 'S',
                inscripciones: [
                  {
                    idInscripto: 7010,
                    idOferta: 310,
                    descripcionOferta: 'Marco legal y tributario',
                    idTurno: 11,
                    idComienzo: 21,
                    fechaInicioComienzo: '2027-03-10',
                    nombreComienzo: 'Marzo 2027',
                    nombreTurno: 'Matutino',
                    fechaReferencia: '2027-03-10',
                  },
                  {
                    idInscripto: 7011,
                    idOferta: 311,
                    descripcionOferta: 'Renta fija y renta variable',
                    idTurno: 12,
                    idComienzo: 22,
                    fechaInicioComienzo: '2027-04-10',
                    nombreComienzo: 'Marzo 2027',
                    nombreTurno: 'Nocturno',
                    fechaReferencia: '2027-04-10',
                  },
                ],
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
      // Producto que el mock no conoce: el Detalle falla, como pasa con productos fuera
      // del catálogo. Retomar tiene que aguantar igual (nunca paso 1).
      if (!['20', '40'].includes(url.searchParams.get('idProducto') ?? '')) {
        return fulfillApiError(route, 'No se encontró la inscripción.');
      }

      // Actualización profesional en proceso: la cabecera no trae idComienzo/idTurno y
      // las ofertas elegidas llegan en `detalle.intereses` (una por seminario).
      if (url.searchParams.get('idProducto') === '40') {
        return fulfillOperation(route, {
          estado: 'En proceso',
          detalle: {
            resumen: { idProducto: 40, carrera: 'Programa de Asesoramiento Financiero' },
            intereses:
              options.inscriptionDetail === 'offers-missing'
                ? []
                : [
                    {
                      idOferta: 310,
                      descripcionOferta: 'Marco legal y tributario',
                      comienzo: 'Marzo 2027',
                      turno: 'Matutino',
                    },
                    {
                      idOferta: 311,
                      descripcionOferta: 'Renta fija y renta variable',
                      comienzo: 'Marzo 2027',
                      turno: 'Nocturno',
                    },
                  ],
          },
        });
      }

      return fulfillOperation(route, {
        estado: 'Pago pendiente',
        pagoPendiente: {
          inscripciones: [
            {
              idInscripcion: 7001,
              idOferta: 300,
              comienzo: 'Marzo 2027',
              turno: 'Matutino',
            },
          ],
          pagoReserva: 15500,
          resumen: {
            idProducto: 20,
            carrera: 'Licenciatura en Diseño Gráfico',
            fechaVencimientoPago: '2027-03-04',
          },
        },
        reservaMinima: {
          tipoPago: 'ABITAB',
          cedula: '12345678',
          codigoPersona: 7001,
          pagoReserva: 15500,
        },
      });
    }

    if (path === '/Inscripciones/EncuestaInicial' && request.method() === 'GET') {
      return fulfillOperation(route, initialSurvey(options.initialSurvey ?? 'empty'));
    }

    if (path === '/Inscripciones/EncuestaInicial' && request.method() === 'POST') {
      return fulfillOperation(route, true);
    }

    if (path === '/Inscripciones/Pagar') {
      return fulfillOperation(route, { resultado: 'Confirmada' });
    }

    if (path === '/Inscripciones/ConfirmarPreInscripcion') {
      return fulfillOperation(route, {
        confirmada: true,
        pagoReserva: 15500,
        estadoCuenta: { saldoActual: 20000 },
        inscripciones: [
          {
            idInscripcion: 7001,
            idOferta: 300,
            comienzo: 'Marzo 2027',
            turno: 'Matutino',
          },
        ],
        resumen: {
          idProducto: 20,
          carrera: 'Licenciatura en Diseño Gráfico',
          fechaVencimientoPago: '2027-03-04',
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
    educacion: {
      ubicacionesUltimoAnioSecundaria: [
        { value: 1, label: 'Uruguay' },
        { value: 2, label: 'En el exterior' },
      ],
      aniosBachillerato: [
        {
          value: 11,
          label: 'Durante secundaria',
          orientaciones: [{ value: 12, label: 'Científico', orientacion: 'Matemática' }],
        },
      ],
      estadosEducacionSuperiorPrevia: [{ value: 3, label: 'No cursé estudios superiores' }],
      universidades: [],
      nivelesFormacionTutores: [{ value: 5, label: 'Universitaria completa' }],
    },
    decisionAcademica: {
      aniosEducacionMediaSuperior: [
        { value: 1, label: 'Durante secundaria' },
        { value: 2, label: '2º EMS (5º año)' },
      ],
      apoyosDecision: [{ value: 1, label: 'Familia' }],
      nivelesDecision: [{ value: 1, label: 'Decidido/a' }],
      universidades: [],
      motivosEleccionOrt: [{ value: 1, label: 'Propuesta académica' }],
    },
    experienciaOrt: { valoraciones: [], publicidadesOrt: [] },
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
